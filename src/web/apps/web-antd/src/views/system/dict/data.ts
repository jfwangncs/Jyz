import type { VbenFormSchema } from '#/adapter/form';
import type { OnActionClickFn, VxeTableGridOptions } from '#/adapter/vxe-table';

import { useAccess } from '@vben/access';

import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';

import { $t } from '#/locales';

dayjs.extend(utc);

export function useDictTypeFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'code',
      label: $t('system.dict.code'),
      rules: 'required',
    },
    {
      component: 'Input',
      fieldName: 'name',
      label: $t('system.dict.name'),
      rules: 'required',
    },
    {
      component: 'Textarea',
      componentProps: { rows: 3 },
      fieldName: 'description',
      label: $t('system.dict.description'),
    },
  ];
}

export function useDictItemFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'code',
      label: $t('system.dict.itemCode'),
      rules: 'required',
    },
    {
      component: 'Input',
      fieldName: 'label',
      label: $t('system.dict.label'),
      rules: 'required',
    },
    {
      component: 'InputNumber',
      fieldName: 'sort',
      label: $t('system.dict.sort'),
      defaultValue: 0,
    },
  ];
}

export function useGridFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'searchKey',
      label: $t('system.dict.searchKey'),
    },
  ];
}

export function useDictTypeColumns<T = any>(
  onActionClick: OnActionClickFn<T>,
  onStatusChange?: (newStatus: any, row: T) => PromiseLike<boolean | undefined>,
): VxeTableGridOptions['columns'] {
  const { hasAccessByCodes } = useAccess();
  return [
    { type: 'expand', width: 50, slots: { content: 'expand' } },
    { field: 'code', title: $t('system.dict.code'), width: 160 },
    { field: 'name', title: $t('system.dict.name'), minWidth: 160 },
    {
      field: 'description',
      title: $t('system.dict.description'),
      minWidth: 200,
      showOverflow: 'tooltip',
    },
    {
      cellRender: {
        attrs: { beforeChange: onStatusChange },
        name:
          hasAccessByCodes(['dict-type:edit']) && onStatusChange
            ? 'CellSwitch'
            : 'CellTag',
      },
      field: 'status',
      title: $t('system.dict.status'),
      width: 100,
    },
    {
      field: 'createdTime',
      formatter: ({ cellValue }) =>
        cellValue
          ? dayjs.utc(cellValue).local().format('YYYY-MM-DD HH:mm:ss')
          : '',
      title: $t('system.dict.createdTime'),
      width: 180,
    },
    {
      cellRender: {
        attrs: {
          onClick: onActionClick,
        },
        name: 'CellOperation',
        options: [
          { code: 'edit', show: () => hasAccessByCodes(['dict-type:edit']) },
        ],
      },
      field: 'operation',
      fixed: 'right',
      title: $t('system.dict.operation'),
      width: 120,
    },
  ];
}

export function useDictItemColumns<T = any>(
  onActionClick: OnActionClickFn<T>,
  onStatusChange?: (newStatus: any, row: T) => PromiseLike<boolean | undefined>,
): VxeTableGridOptions['columns'] {
  return [
    { field: 'code', title: $t('system.dict.itemCode'), width: 140 },
    { field: 'label', title: $t('system.dict.label'), minWidth: 140 },
    { field: 'sort', title: $t('system.dict.sort'), width: 80 },
    {
      cellRender: {
        attrs: { beforeChange: onStatusChange },
        name: onStatusChange ? 'CellSwitch' : 'CellTag',
      },
      field: 'status',
      title: $t('system.dict.status'),
      width: 100,
    },
    {
      cellRender: {
        attrs: {
          onClick: onActionClick,
        },
        name: 'CellOperation',
        options: ['edit'],
      },
      field: 'operation',
      fixed: 'right',
      title: $t('system.dict.operation'),
      width: 120,
    },
  ];
}
