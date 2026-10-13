import type { VbenFormSchema } from '#/adapter/form';
import type { OnActionClickFn, VxeTableGridOptions } from '#/adapter/vxe-table';
import type { SystemUserApi } from '#/api';

import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';

import { $t } from '#/locales';

dayjs.extend(utc);

export function useFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'nickName',
      label: $t('system.user.nickName'),
    },
    {
      component: 'Input',
      fieldName: 'realName',
      label: $t('system.user.realName'),
    },
    {
      component: 'Input',
      fieldName: 'phone',
      label: $t('system.user.phone'),
    },
    {
      component: 'Input',
      fieldName: 'avatar',
      label: $t('system.user.avatar'),
    },
    {
      component: 'Select',
      componentProps: {
        allowClear: true,
        options: [
          { label: $t('system.user.unknown'), value: 0 },
          { label: $t('system.user.male'), value: 1 },
          { label: $t('system.user.female'), value: 2 },
        ],
      },
      fieldName: 'gender',
      label: $t('system.user.gender'),
    },
    {
      component: 'Input',
      fieldName: 'province',
      label: $t('system.user.province'),
    },
    {
      component: 'Input',
      fieldName: 'city',
      label: $t('system.user.city'),
    },
    {
      component: 'Input',
      fieldName: 'country',
      label: $t('system.user.country'),
    },
  ];
}

export function useGridFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'searchKey',
      label: $t('system.user.userName'),
    },
    {
      component: 'Select',
      componentProps: {
        allowClear: true,
        options: [
          { label: $t('common.enabled'), value: 1 },
          { label: $t('common.disabled'), value: 0 },
        ],
      },
      fieldName: 'status',
      label: $t('system.user.status'),
    },
    {
      component: 'RangePicker',
      componentProps: {
        showTime: true,
        format: 'YYYY-MM-DD HH:mm:ss',
        valueFormat: 'YYYY-MM-DD HH:mm:ss',
      },
      fieldName: 'createdTime',
      label: $t('system.user.createdTime'),
    },
  ];
}

export function useColumns<T = SystemUserApi.SystemUser>(
  onActionClick: OnActionClickFn<T>,
  onStatusChange?: (newStatus: any, row: T) => PromiseLike<boolean | undefined>,
): VxeTableGridOptions['columns'] {
  return [
    {
      field: 'id',
      title: $t('system.user.id'),
      width: 80,
    },
    {
      align: 'center',
      cellRender: { name: 'CellImage', props: { size: 50 } },
      field: 'avatar',
      title: $t('system.user.avatar'),
      width: 70,
    },
    {
      field: 'userName',
      title: $t('system.user.userName'),
      width: 130,
    },
    {
      field: 'nickName',
      title: $t('system.user.nickName'),
      width: 120,
    },
    {
      field: 'realName',
      minWidth: 120,
      title: $t('system.user.realName'),
    },
    {
      field: 'phone',
      title: $t('system.user.phone'),
      width: 130,
    },
    {
      field: 'gender',
      formatter: ({ cellValue }) => {
        if (cellValue === 0) return $t('system.user.unknown');
        if (cellValue === 1) return $t('system.user.male');
        if (cellValue === 2) return $t('system.user.female');
        return '';
      },
      title: $t('system.user.gender'),
      width: 80,
    },
    {
      field: 'province',
      title: $t('system.user.province'),
      width: 100,
    },
    {
      field: 'city',
      width: 100,
      title: $t('system.user.city'),
    },
    {
      field: 'roles',
      minWidth: 120,
      slots: { default: 'roles' },
      title: $t('system.user.roles'),
    },
    {
      cellRender: {
        attrs: { beforeChange: onStatusChange },
        name: onStatusChange ? 'CellSwitch' : 'CellTag',
      },
      field: 'status',
      title: $t('system.user.status'),
      width: 100,
    },
    {
      field: 'lastLoginTime',
      formatter: ({ cellValue }) =>
        cellValue
          ? dayjs.utc(cellValue).local().format('YYYY-MM-DD HH:mm:ss')
          : '',
      title: $t('system.user.lastLoginTime'),
      width: 180,
    },
    {
      field: 'createdTime',
      formatter: ({ cellValue }) =>
        cellValue
          ? dayjs.utc(cellValue).local().format('YYYY-MM-DD HH:mm:ss')
          : '',
      title: $t('system.user.createdTime'),
      width: 180,
    },
    {
      align: 'center',
      cellRender: {
        attrs: {
          nameField: 'userName',
          nameTitle: $t('system.user.name'),
          onClick: onActionClick,
        },
        name: 'CellOperation',
        options: ['edit'],
      },
      field: 'operation',
      fixed: 'right',
      title: $t('system.user.operation'),
      width: 130,
    },
  ];
}
