import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridOptions } from '#/adapter/vxe-table';

import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';

import { $t } from '#/locales';

dayjs.extend(utc);

export function useGridFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'searchKey',
      label: $t('system.loginLog.searchKey'),
    },
    {
      component: 'Select',
      componentProps: {
        allowClear: true,
        options: [
          { label: $t('system.loginLog.success'), value: 0 },
          { label: $t('system.loginLog.failed'), value: 1 },
        ],
      },
      fieldName: 'result',
      label: $t('system.loginLog.result'),
    },
    {
      component: 'RangePicker',
      componentProps: {
        showTime: true,
        format: 'YYYY-MM-DD HH:mm:ss',
        valueFormat: 'YYYY-MM-DD HH:mm:ss',
      },
      fieldName: 'createdTime',
      label: $t('system.loginLog.createdTime'),
    },
  ];
}

export function useColumns(): VxeTableGridOptions['columns'] {
  return [
    { field: 'id', title: $t('system.loginLog.id'), width: 80 },
    { field: 'userName', title: $t('system.loginLog.userName'), width: 130 },
    { field: 'ip', title: $t('system.loginLog.ip'), width: 130 },
    {
      field: 'result',
      title: $t('system.loginLog.result'),
      width: 100,
      formatter: ({ cellValue }) =>
        cellValue === 0
          ? $t('system.loginLog.success')
          : $t('system.loginLog.failed'),
    },
    {
      field: 'failReason',
      minWidth: 180,
      showOverflow: 'tooltip',
      title: $t('system.loginLog.failReason'),
    },
    {
      field: 'userAgent',
      minWidth: 200,
      showOverflow: 'tooltip',
      title: $t('system.loginLog.userAgent'),
    },
    {
      field: 'createdTime',
      formatter: ({ cellValue }) =>
        cellValue
          ? dayjs.utc(cellValue).local().format('YYYY-MM-DD HH:mm:ss')
          : '',
      title: $t('system.loginLog.createdTime'),
      width: 180,
    },
  ];
}
