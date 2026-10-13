<script lang="ts" setup>
import type { VxeTableGridOptions } from '#/adapter/vxe-table';
import type { SystemDictApi } from '#/api';

import { useAccess } from '@vben/access';
import { Page, useVbenDrawer } from '@vben/common-ui';
import { Plus } from '@vben/icons';

import { Button, Modal } from 'ant-design-vue';

import { useVbenVxeGrid } from '#/adapter/vxe-table';
import { getDictTypeList, updateDictType } from '#/api';
import { $t } from '#/locales';

import { useDictTypeColumns, useGridFormSchema } from './data';
import ItemsPanel from './modules/items-panel.vue';
import TypeForm from './modules/type-form.vue';

const { hasAccessByCodes } = useAccess();

const [TypeFormDrawer, typeFormDrawerApi] = useVbenDrawer({
  connectedComponent: TypeForm,
  destroyOnClose: true,
});

function confirm(content: string, title: string) {
  return new Promise<void>((resolve, reject) => {
    Modal.confirm({
      content,
      onCancel() {
        reject(new Error('cancelled'));
      },
      onOk() {
        resolve();
      },
      title,
    });
  });
}

async function onTypeStatusChange(
  newStatus: number,
  row: SystemDictApi.DictType,
): Promise<boolean> {
  const label = newStatus === 1 ? $t('common.enabled') : $t('common.disabled');
  try {
    await confirm(
      `${$t('system.common.prefix-change')} ${row.name} ${$t('system.common.mid-change')} ${$t('system.dict.status')} ${$t('system.common.suffix-change')}【 ${label}】?`,
      $t('common.edit'),
    );
    await updateDictType(row.id, { status: newStatus });
    return true;
  } catch {
    return false;
  }
}

function onTypeActionClick(e: { code: string; row: SystemDictApi.DictType }) {
  if (e.code === 'edit') typeFormDrawerApi.setData(e.row).open();
}

const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns: useDictTypeColumns(onTypeActionClick, onTypeStatusChange),
    height: 'auto',
    expandConfig: { trigger: 'row', accordion: false },
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) =>
          getDictTypeList({
            pageIndex: page.currentPage,
            pageSize: page.pageSize,
            ...formValues,
          }),
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: {
      custom: true,
      export: false,
      refresh: true,
      search: true,
      zoom: false,
    },
  } as VxeTableGridOptions<SystemDictApi.DictType>,
  formOptions: { schema: useGridFormSchema(), submitOnChange: true },
});

function onRefresh() {
  gridApi.query();
}
</script>

<template>
  <Page auto-content-height>
    <TypeFormDrawer @success="onRefresh" />
    <Grid>
      <template #toolbar-actions>
        <Button
          v-if="hasAccessByCodes(['dict-type:add'])"
          type="primary"
          @click="typeFormDrawerApi.open()"
        >
          <Plus class="size-5" />
          {{ $t('system.dict.addType') }}
        </Button>
      </template>
      <template #expand="{ row }">
        <ItemsPanel :row="row" />
      </template>
    </Grid>
  </Page>
</template>
