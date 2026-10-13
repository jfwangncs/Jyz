<script lang="ts" setup>
import type {
  OnActionClickParams,
  VxeTableGridOptions,
} from '#/adapter/vxe-table';
import type { SystemRedisCacheApi } from '#/api';

import { useAccess } from '@vben/access';
import { Page } from '@vben/common-ui';
import { IconifyIcon } from '@vben/icons';

import { Button, Modal, Tag } from 'ant-design-vue';

import { useVbenVxeGrid } from '#/adapter/vxe-table';
import { deleteRedisCacheKeys, getRedisCacheList } from '#/api';
import { $t } from '#/locales';

import { useColumns, useGridFormSchema } from './data';

const { hasAccessByCodes } = useAccess();

const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    checkboxConfig: { key: 'key' },
    columns: useColumns(onActionClick),
    height: 'auto',
    pagerConfig: { pageSize: 20, pageSizes: [10, 20, 50, 100] },
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => {
          const all = await getRedisCacheList();
          const { searchKey } = formValues;
          const filtered = searchKey
            ? all.filter((i) =>
                i.key.toLowerCase().includes(searchKey.toLowerCase()),
              )
            : all;
          const start = (page.currentPage - 1) * page.pageSize;
          return {
            items: filtered.slice(start, start + page.pageSize),
            total: filtered.length,
          };
        },
      },
    },
    rowConfig: { keyField: 'key' },
    toolbarConfig: {
      custom: true,
      export: false,
      refresh: true,
      search: true,
      zoom: false,
    },
  } as VxeTableGridOptions<SystemRedisCacheApi.RedisCacheItem>,
  formOptions: {
    schema: useGridFormSchema(),
    submitOnChange: true,
  },
});

async function onActionClick(
  e: OnActionClickParams<SystemRedisCacheApi.RedisCacheItem>,
) {
  if (e.code === 'delete') {
    await deleteRedisCacheKeys([e.row.key]);
    gridApi.query();
  }
}

async function handleBatchDelete() {
  const rows =
    gridApi.grid?.getCheckboxRecords() as SystemRedisCacheApi.RedisCacheItem[];
  if (!rows?.length) return;

  Modal.confirm({
    content: $t('system.redisCache.confirmBatchDelete', [rows.length]),
    onOk: async () => {
      await deleteRedisCacheKeys(rows.map((r) => r.key));
      gridApi.query();
    },
    title: $t('system.redisCache.batchDelete'),
  });
}

function formatTtl(ttl: number): { color: string; text: string } {
  if (ttl === -1)
    return { color: 'green', text: $t('system.redisCache.noExpiry') };
  if (ttl < 60) return { color: 'red', text: `${ttl}s` };
  const h = Math.floor(ttl / 3600);
  const m = Math.floor((ttl % 3600) / 60);
  const s = ttl % 60;
  const parts: string[] = [];
  if (h > 0) parts.push(`${h}h`);
  if (m > 0) parts.push(`${m}m`);
  if (s > 0) parts.push(`${s}s`);
  return { color: 'default', text: parts.join(' ') };
}
</script>

<template>
  <Page auto-content-height>
    <Grid>
      <template #toolbar-actions>
        <Button
          v-if="hasAccessByCodes(['redis-cache:delete'])"
          danger
          type="primary"
          @click="handleBatchDelete"
        >
          <IconifyIcon class="size-5" icon="ant-design:delete-outlined" />
          {{ $t('system.redisCache.batchDelete') }}
        </Button>
      </template>
      <template #ttl="{ row }">
        <Tag :color="formatTtl(row.ttl).color">
          {{ formatTtl(row.ttl).text }}
        </Tag>
      </template>
    </Grid>
  </Page>
</template>
