<script lang="ts" setup>
import type { Recordable } from '@vben/types';

import type { SystemPermissionApi } from '#/api/system/permission';

import { computed, h, nextTick, ref } from 'vue';

import { useVbenDrawer } from '@vben/common-ui';
import { IconifyIcon } from '@vben/icons';

import { useVbenForm } from '#/adapter/form';
import {
  createPermission,
  getPermissionTreeList,
  updatePermission,
} from '#/api/system/permission';
import { $t } from '#/locales';

import { buildParentTree, useFormSchema } from '../data';

const emits = defineEmits(['success']);

const id = ref<number>();
const formData = ref<SystemPermissionApi.SystemPermission>();

const [Form, formApi] = useVbenForm({
  schema: [
    ...useFormSchema(),
    {
      component: 'ApiTreeSelect',
      componentProps: {
        afterFetch: (data: SystemPermissionApi.SystemPermission[]) =>
          buildParentTree(data),
        allowClear: true,
        alwaysLoad: true,
        api: getPermissionTreeList,
        childrenField: 'children',
        class: 'w-full',
        filterTreeNode(input: string, node: Recordable<any>) {
          if (!input || input.length === 0) return true;
          const name: string = node.name ?? '';
          return name.toLowerCase().includes(input.toLowerCase());
        },
        labelField: 'name',
        placeholder: $t('system.permission.parent'),
        showSearch: true,
        treeDefaultExpandAll: true,
        valueField: 'id',
      },
      fieldName: 'parentId',
      label: $t('system.permission.parent'),
      renderComponentContent() {
        return {
          title({ label, icon }: { icon?: string; label: string }) {
            const coms = [];
            if (!label) return '';
            if (icon) {
              coms.push(h(IconifyIcon, { class: 'size-4', icon }));
            }
            coms.push(h('span', {}, label));
            return h('div', { class: 'flex items-center gap-1' }, coms);
          },
        };
      },
    },
  ],
  showDefaultActions: false,
});

const [Drawer, drawerApi] = useVbenDrawer({
  async onConfirm() {
    const { valid } = await formApi.validate();
    if (!valid) return;
    const values = await formApi.getValues();
    drawerApi.lock();
    const request = id.value
      ? updatePermission(
          id.value,
          values as SystemPermissionApi.UpdatePermissionParams,
        )
      : createPermission(values as SystemPermissionApi.CreatePermissionParams);
    request
      .then(() => {
        emits('success');
        drawerApi.close();
      })
      .catch(() => {
        drawerApi.unlock();
      });
  },

  async onOpenChange(isOpen) {
    if (!isOpen) return;
    formApi.resetForm();
    const data = drawerApi.getData<SystemPermissionApi.SystemPermission>();
    if (data?.id) {
      // edit mode
      formData.value = data;
      id.value = data.id;
      await formApi.updateSchema([
        { disabled: true, fieldName: 'code' },
        {
          componentProps: {
            afterFetch: (items: SystemPermissionApi.SystemPermission[]) =>
              buildParentTree(items, data.id),
          },
          fieldName: 'parentId',
        },
      ]);
      await nextTick();
      formApi.setValues({
        name: data.name,
        code: data.code,
        type: data.type,
        description: data.description,
        icon: data.icon,
        sort: data.sort,
        status: data.status,
        parentId: data.parentId,
      });
    } else {
      // create mode (or append with parentId only)
      id.value = undefined;
      formData.value = undefined;
      await formApi.updateSchema([
        { disabled: false, fieldName: 'code' },
        {
          componentProps: {
            afterFetch: (items: SystemPermissionApi.SystemPermission[]) =>
              buildParentTree(items),
          },
          fieldName: 'parentId',
        },
      ]);
      if (data?.parentId) {
        await nextTick();
        formApi.setValues({ parentId: data.parentId });
      }
    }
  },
});

const getDrawerTitle = computed(() =>
  id.value
    ? $t('common.edit', [$t('system.permission.name')])
    : $t('common.create', [$t('system.permission.name')]),
);
</script>

<template>
  <Drawer :title="getDrawerTitle">
    <Form />
  </Drawer>
</template>
