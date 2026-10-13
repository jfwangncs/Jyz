<script lang="ts" setup>
import type {
  OnActionClickParams,
  VxeTableGridOptions,
} from '#/adapter/vxe-table';
import type { SystemUserApi } from '#/api';

import { Page, useVbenDrawer } from '@vben/common-ui';

import { Modal, Tag } from 'ant-design-vue';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';

import { useVbenVxeGrid } from '#/adapter/vxe-table';
import { getUserList, updateUser } from '#/api';
import { $t } from '#/locales';

import { useColumns, useGridFormSchema } from './data';
import Form from './modules/form.vue';

dayjs.extend(utc);

const [FormDrawer, formDrawerApi] = useVbenDrawer({
  connectedComponent: Form,
  destroyOnClose: true,
});

const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns: useColumns(onActionClick, onStatusChange),
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => {
          const { startTime, endTime, ...rest } = formValues;
          return await getUserList({
            pageIndex: page.currentPage,
            pageSize: page.pageSize,
            ...(startTime && {
              startTime: dayjs(startTime).utc().format('YYYY-MM-DD HH:mm:ss'),
            }),
            ...(endTime && {
              endTime: dayjs(endTime).utc().format('YYYY-MM-DD HH:mm:ss'),
            }),
            ...rest,
          });
        },
      },
    },
    rowConfig: {
      keyField: 'id',
    },
    toolbarConfig: {
      custom: true,
      export: false,
      refresh: true,
      search: true,
      zoom: false,
    },
  } as VxeTableGridOptions<SystemUserApi.SystemUser>,
  formOptions: {
    fieldMappingTime: [
      ['createdTime', ['startTime', 'endTime'], 'YYYY-MM-DD HH:mm:ss'],
    ],
    schema: useGridFormSchema(),
    submitOnChange: true,
  },
});

function onActionClick(e: OnActionClickParams<SystemUserApi.SystemUser>) {
  if (e.code === 'edit') {
    formDrawerApi.setData(e.row).open();
  }
}

function confirm(content: string, title: string) {
  return new Promise((resolve, reject) => {
    Modal.confirm({
      content,
      onCancel() {
        reject(new Error('cancelled'));
      },
      onOk() {
        resolve(true);
      },
      title,
    });
  });
}

async function onStatusChange(
  newStatus: number,
  row: SystemUserApi.SystemUser,
): Promise<boolean> {
  const label = newStatus === 1 ? $t('common.enabled') : $t('common.disabled');
  try {
    await confirm(
      `${$t('system.common.prefix-change')} ${row.userName} ${$t('system.common.mid-change')} ${$t('system.user.status')} ${$t('system.common.suffix-change')}【 ${label}】?`,
      $t('common.edit'),
    );

    await updateUser(row.id, { status: newStatus });
    return true;
  } catch {
    return false;
  }
}

function onRefresh() {
  gridApi.query();
}
</script>

<template>
  <Page auto-content-height>
    <FormDrawer @success="onRefresh" />
    <Grid :table-title="$t('system.user.list')">
      <template #roles="{ row }">
        <div class="flex flex-wrap gap-1">
          <Tag v-for="role in row.roleList" :key="role.id" color="blue">
            {{ role.name }}
          </Tag>
        </div>
      </template>
    </Grid>
  </Page>
</template>
