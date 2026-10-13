<script lang="ts" setup>
import type { DataNode } from 'ant-design-vue/es/tree';

import type { Recordable } from '@vben/types';

import type { SystemPermissionApi } from '#/api/system/permission';
import type { SystemRoleApi } from '#/api/system/role';

import { computed, nextTick, ref } from 'vue';

import { Tree, useVbenDrawer } from '@vben/common-ui';

import { Spin } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { getPermissionTreeList } from '#/api/system/permission';
import {
  assignRolePermissions,
  createRole,
  updateRole,
} from '#/api/system/role';
import { $t } from '#/locales';

import { useFormSchema } from '../data';

const emits = defineEmits(['success']);

const formData = ref<SystemRoleApi.SystemRole>();

const [Form, formApi] = useVbenForm({
  schema: useFormSchema(),
  showDefaultActions: false,
});

const permissions = ref<DataNode[]>([]);
const loadingPermissions = ref(false);

const id = ref();
const [Drawer, drawerApi] = useVbenDrawer({
  async onConfirm() {
    const { valid } = await formApi.validate();
    if (!valid) return;
    const values = await formApi.getValues();
    const permissionIds: number[] = (values.permissions as number[]) ?? [];
    drawerApi.lock();
    try {
      let roleId: number;
      if (id.value) {
        await updateRole(id.value, values);
        roleId = Number(id.value);
      } else {
        const created = await createRole(values);
        roleId = Number(created.id);
      }
      await assignRolePermissions(roleId, permissionIds);
      emits('success');
      drawerApi.close();
    } catch {
      drawerApi.unlock();
    }
  },

  async onOpenChange(isOpen) {
    if (isOpen) {
      const data = drawerApi.getData<SystemRoleApi.SystemRole>();
      formApi.resetForm();

      if (data) {
        formData.value = data;
        id.value = data.id;
      } else {
        id.value = undefined;
      }

      if (permissions.value.length === 0) {
        await loadPermissions();
      }
      // Wait for Vue to flush DOM updates (form fields mounted)
      await nextTick();
      if (data) {
        const ids = (data.permissions ?? []).map((p: any) => p.id);
        formApi.setValues({ ...data, permissions: ids });
      }
    }
  },
});

async function loadPermissions() {
  loadingPermissions.value = true;
  try {
    const flat = await getPermissionTreeList();
    permissions.value = buildTree(flat) as any[];
  } finally {
    loadingPermissions.value = false;
  }
}

function buildTree(
  items: SystemPermissionApi.SystemPermission[],
  parentId: null | number = null,
): SystemPermissionApi.SystemPermission[] {
  return items
    .filter((p) => (p.parentId ?? null) === parentId)
    .toSorted((a, b) => (a.sort ?? 0) - (b.sort ?? 0))
    .map((p) => {
      const children = buildTree(items, p.id);
      return children.length > 0 ? { ...p, children } : { ...p };
    });
}

const getDrawerTitle = computed(() => {
  return formData.value?.id
    ? $t('common.edit', $t('system.role.name'))
    : $t('common.create', $t('system.role.name'));
});

function getNodeClass(node: Recordable<any>) {
  const classes: string[] = [];
  if (node.value?.type === 'button') {
    classes.push('inline-flex');
  }

  return classes.join(' ');
}
</script>
<template>
  <Drawer :title="getDrawerTitle">
    <Form>
      <template #permissions="slotProps">
        <Spin :spinning="loadingPermissions" wrapper-class-name="w-full">
          <Tree
            :tree-data="permissions"
            multiple
            bordered
            :default-expanded-level="2"
            :get-node-class="getNodeClass"
            v-bind="slotProps"
            value-field="id"
            label-field="name"
            icon-field="icon"
          />
        </Spin>
      </template>
    </Form>
  </Drawer>
</template>
<style lang="css" scoped>
:deep(.ant-tree-title) {
  .tree-actions {
    display: none;
    margin-left: 20px;
  }
}

:deep(.ant-tree-title:hover) {
  .tree-actions {
    display: flex;
    flex: auto;
    justify-content: flex-end;
    margin-left: 20px;
  }
}
</style>
