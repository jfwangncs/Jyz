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
      label: $t('system.auditLog.searchKey'),
    },
    {
      component: 'Input',
      fieldName: 'resource',
      label: $t('system.auditLog.resource'),
    },
    {
      component: 'Select',
      componentProps: {
        allowClear: true,
        options: [
          { label: $t('system.auditLog.added'), value: 'Added' },
          { label: $t('system.auditLog.modified'), value: 'Modified' },
          { label: $t('system.auditLog.deleted'), value: 'Deleted' },
        ],
      },
      fieldName: 'action',
      label: $t('system.auditLog.action'),
    },
    {
      component: 'RangePicker',
      componentProps: {
        showTime: true,
        format: 'YYYY-MM-DD HH:mm:ss',
        valueFormat: 'YYYY-MM-DD HH:mm:ss',
      },
      fieldName: 'createdTime',
      label: $t('system.auditLog.createdTime'),
    },
  ];
}

export function useColumns(): VxeTableGridOptions['columns'] {
  return [
    { field: 'id', title: $t('system.auditLog.id'), width: 80 },
    { field: 'userName', title: $t('system.auditLog.userName'), width: 130 },
    { field: 'action', title: $t('system.auditLog.action'), width: 100 },
    { field: 'resource', title: $t('system.auditLog.resource'), width: 130 },
    {
      field: 'resourceId',
      title: $t('system.auditLog.resourceId'),
      width: 100,
    },
    {
      field: 'oldValue',
      minWidth: 200,
      showOverflow: 'tooltip',
      title: $t('system.auditLog.oldValue'),
    },
    {
      field: 'newValue',
      minWidth: 200,
      showOverflow: 'tooltip',
      title: $t('system.auditLog.newValue'),
    },
    { field: 'ip', title: $t('system.auditLog.ip'), width: 130 },
    {
      field: 'createdTime',
      formatter: ({ cellValue }) =>
        cellValue
          ? dayjs.utc(cellValue).local().format('YYYY-MM-DD HH:mm:ss')
          : '',
      title: $t('system.auditLog.createdTime'),
      width: 180,
    },
  ];
}
