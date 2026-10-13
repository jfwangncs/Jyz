import type { VbenFormSchema } from '#/adapter/form';
import type { OnActionClickFn, VxeTableGridOptions } from '#/adapter/vxe-table';
import type { SystemPermissionApi } from '#/api/system/permission';

import { $t } from '#/locales';

/**
 * Build a nested tree for the parent permission selector.
 * Only menus (type=1) and directories (type=3) can be parents.
 * Optionally excludes a specific id (e.g. the item being edited).
 */
export function buildParentTree(
  items: SystemPermissionApi.SystemPermission[],
  excludeId?: number,
  parentId: null | number = null,
): SystemPermissionApi.SystemPermission[] {
  const eligible = items.filter(
    (p) => (p.type === 1 || p.type === 3) && p.id !== excludeId,
  );
  return eligible
    .filter((p) => (p.parentId ?? null) === parentId)
    .toSorted((a, b) => (a.sort ?? 0) - (b.sort ?? 0))
    .map((p) => {
      const children = buildParentTree(items, excludeId, p.id);
      return children.length > 0 ? { ...p, children } : { ...p };
    });
}

export function getPermissionTypeOptions() {
  return [
    { color: 'blue', label: $t('system.permission.typeMenu'), value: 1 },
    { color: 'orange', label: $t('system.permission.typeButton'), value: 2 },
    { color: 'purple', label: $t('system.permission.typeDirectory'), value: 3 },
    { color: 'cyan', label: $t('system.permission.typeLink'), value: 4 },
  ];
}

export function useFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'RadioGroup',
      componentProps: {
        buttonStyle: 'solid',
        options: getPermissionTypeOptions(),
        optionType: 'button',
      },
      defaultValue: 1,
      fieldName: 'type',
      label: $t('system.permission.type'),
    },
    {
      component: 'Input',
      fieldName: 'name',
      label: $t('system.permission.name'),
      rules: 'required',
    },
    {
      component: 'Input',
      fieldName: 'code',
      label: $t('system.permission.code'),
      rules: 'required',
    },
    {
      component: 'Input',
      fieldName: 'description',
      label: $t('system.permission.description'),
    },
    {
      component: 'IconPicker',
      dependencies: {
        show: (values) => [1, 3, 4].includes(values.type),
        triggerFields: ['type'],
      },
      fieldName: 'icon',
      label: $t('system.permission.icon'),
    },
    {
      component: 'InputNumber',
      componentProps: { min: 0 },
      fieldName: 'sort',
      label: $t('system.permission.sort'),
    },
    {
      component: 'RadioGroup',
      componentProps: {
        buttonStyle: 'solid',
        options: [
          { label: $t('common.enabled'), value: 1 },
          { label: $t('common.disabled'), value: 0 },
        ],
        optionType: 'button',
      },
      defaultValue: 1,
      fieldName: 'status',
      label: $t('system.permission.status'),
    },
  ];
}

export function useGridFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'searchKey',
      label: $t('system.permission.codeOrName'),
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
      label: $t('system.permission.status'),
    },
  ];
}

export function useColumns<T = SystemPermissionApi.SystemPermission>(
  onActionClick: OnActionClickFn<T>,
  onStatusChange?: (newStatus: any, row: T) => PromiseLike<boolean | undefined>,
): VxeTableGridOptions['columns'] {
  return [
    {
      align: 'left',
      field: 'name',
      fixed: 'left',
      slots: { default: 'name' },
      title: $t('system.permission.name'),
      treeNode: true,
      width: 220,
    },
    {
      cellRender: { name: 'CellTag', options: getPermissionTypeOptions() },
      field: 'type',
      title: $t('system.permission.type'),
      width: 100,
    },
    {
      field: 'code',
      title: $t('system.permission.code'),
      width: 220,
    },
    {
      field: 'sort',
      title: $t('system.permission.sort'),
      width: 80,
    },
    {
      field: 'icon',
      title: $t('system.permission.icon'),
      width: 120,
    },
    {
      field: 'description',
      minWidth: 100,
      title: $t('system.permission.description'),
    },
    {
      cellRender: {
        attrs: { beforeChange: onStatusChange },
        name: onStatusChange ? 'CellSwitch' : 'CellTag',
      },
      field: 'status',
      title: $t('system.permission.status'),
      width: 120,
    },
    {
      align: 'center',
      cellRender: {
        attrs: {
          nameField: 'name',
          nameTitle: $t('system.permission.name'),
          onClick: onActionClick,
        },
        name: 'CellOperation',
        options: [
          { code: 'append', text: $t('system.permission.addChild') },
          'edit',
        ],
      },
      field: 'operation',
      fixed: 'right',
      title: $t('system.permission.operation'),
      width: 200,
    },
  ];
}
