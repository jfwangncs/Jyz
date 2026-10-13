<script lang="ts" setup>
import type {
  OnActionClickParams,
  VxeTableGridOptions,
} from '#/adapter/vxe-table';
import type { SystemRoleApi } from '#/api';

import { useAccess } from '@vben/access';
import { Page, useVbenDrawer } from '@vben/common-ui';
import { Plus } from '@vben/icons';

import { Button, Modal } from 'ant-design-vue';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';

import { useVbenVxeGrid } from '#/adapter/vxe-table';
import { getRoleList, updateRole } from '#/api';
import { $t } from '#/locales';

import { useColumns, useGridFormSchema } from './data';
import Form from './modules/form.vue';

dayjs.extend(utc);

const { hasAccessByCodes } = useAccess();
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
          return await getRoleList({
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
  } as VxeTableGridOptions<SystemRoleApi.SystemRole>,
  formOptions: {
    fieldMappingTime: [
      ['createdTime', ['startTime', 'endTime'], 'YYYY-MM-DD HH:mm:ss'],
    ],
    schema: useGridFormSchema(),
    submitOnChange: true,
  },
});

function onActionClick(e: OnActionClickParams<SystemRoleApi.SystemRole>) {
  if (e.code === 'edit') {
    onEdit(e.row);
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
  row: SystemRoleApi.SystemRole,
): Promise<boolean> {
  const label = newStatus === 1 ? $t('common.enabled') : $t('common.disabled');
  try {
    await confirm(
      `${$t('system.common.prefix-change')} ${row.name} ${$t('system.common.mid-change')} ${$t('system.role.status')} ${$t('system.common.suffix-change')}【 ${label}】?`,
      $t('common.edit'),
    );
    await updateRole(row.id, { status: newStatus });
    return true;
  } catch {
    return false;
  }
}

function onEdit(row: SystemRoleApi.SystemRole) {
  formDrawerApi.setData(row).open();
}

function onRefresh() {
  gridApi.query();
}

function onCreate() {
  formDrawerApi.setData({}).open();
}
</script>
<template>
  <Page auto-content-height>
    <FormDrawer @success="onRefresh" />
    <Grid :table-title="$t('system.role.list')">
      <template #toolbar-tools>
        <Button
          v-if="hasAccessByCodes(['role:add'])"
          type="primary"
          @click="onCreate"
        >
          <Plus class="size-5" />
          {{ $t('ui.actionTitle.create', [$t('system.role.name')]) }}
        </Button>
      </template>
    </Grid>
  </Page>
</template>
