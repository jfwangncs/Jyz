<script lang="ts" setup>
import type { VxeTableGridOptions } from '#/adapter/vxe-table';
import type { SystemLoginLogApi } from '#/api';

import { Page } from '@vben/common-ui';

import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';

import { useVbenVxeGrid } from '#/adapter/vxe-table';
import { getLoginLogList } from '#/api';

import { useColumns, useGridFormSchema } from './data';

dayjs.extend(utc);

const [Grid] = useVbenVxeGrid({
  gridOptions: {
    columns: useColumns(),
    height: 'auto',
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => {
          const { startTime, endTime, ...rest } = formValues;
          return await getLoginLogList({
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
    rowConfig: { keyField: 'id' },
    toolbarConfig: {
      custom: true,
      export: false,
      refresh: true,
      search: true,
      zoom: false,
    },
  } as VxeTableGridOptions<SystemLoginLogApi.LoginLog>,
  formOptions: {
    fieldMappingTime: [
      ['createdTime', ['startTime', 'endTime'], 'YYYY-MM-DD HH:mm:ss'],
    ],
    schema: useGridFormSchema(),
    submitOnChange: true,
  },
});
</script>

<template>
  <Page auto-content-height>
    <Grid :table-title="$t('system.loginLog.list')" />
  </Page>
</template>
