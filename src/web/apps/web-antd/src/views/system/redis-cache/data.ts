import type { VbenFormSchema } from '#/adapter/form';
import type { OnActionClickFn, VxeTableGridOptions } from '#/adapter/vxe-table';

import { useAccess } from '@vben/access';

import { $t } from '#/locales';

export function useGridFormSchema(): VbenFormSchema[] {
  return [
    {
      component: 'Input',
      fieldName: 'searchKey',
      label: $t('system.redisCache.key'),
    },
  ];
}

export function useColumns<T = any>(
  onActionClick: OnActionClickFn<T>,
): VxeTableGridOptions['columns'] {
  const { hasAccessByCodes } = useAccess();
  return [
    { type: 'checkbox', width: 50 },
    {
      field: 'key',
      minWidth: 300,
      showOverflow: 'tooltip',
      title: $t('system.redisCache.key'),
    },
    {
      field: 'ttl',
      slots: { default: 'ttl' },
      title: $t('system.redisCache.ttl'),
      width: 130,
    },
    {
      field: 'value',
      minWidth: 200,
      showOverflow: 'tooltip',
      title: $t('system.redisCache.value'),
    },
    {
      cellRender: {
        attrs: {
          nameField: 'key',
          nameTitle: $t('system.redisCache.key'),
          onClick: onActionClick,
        },
        name: 'CellOperation',
        options: [
          {
            code: 'delete',
            show: () => hasAccessByCodes(['redis-cache:delete']),
          },
        ],
      },
      field: 'operation',
      fixed: 'right',
      title: $t('system.redisCache.operation'),
      width: 100,
    },
  ];
}
