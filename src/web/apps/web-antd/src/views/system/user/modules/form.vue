<script lang="ts" setup>
import type { SystemUserApi } from '#/api';

import { computed, nextTick, ref } from 'vue';

import { useVbenDrawer } from '@vben/common-ui';

import { useVbenForm } from '#/adapter/form';
import { updateUser } from '#/api';
import { getAllRoleList } from '#/api/system/role';
import { $t } from '#/locales';

import { useFormSchema } from '../data';

const emits = defineEmits(['success']);

const formData = ref<SystemUserApi.SystemUser>();

const [Form, formApi] = useVbenForm({
  schema: [
    ...useFormSchema(),
    {
      component: 'ApiSelect',
      componentProps: {
        alwaysLoad: true,
        api: getAllRoleList,
        class: 'w-full',
        labelField: 'name',
        mode: 'multiple',
        placeholder: $t('system.user.roles'),
        valueField: 'id',
      },
      fieldName: 'roleIds',
      label: $t('system.user.roles'),
    },
  ],
  showDefaultActions: false,
});

const id = ref<number>();

const [Drawer, drawerApi] = useVbenDrawer({
  async onConfirm() {
    const { valid } = await formApi.validate();
    if (!valid) return;
    const values = await formApi.getValues();
    drawerApi.lock();
    try {
      await updateUser(id.value!, values);
      emits('success');
      drawerApi.close();
    } catch {
      drawerApi.unlock();
    }
  },

  async onOpenChange(isOpen) {
    if (isOpen) {
      const data = drawerApi.getData<SystemUserApi.SystemUser>();
      formApi.resetForm();
      formData.value = data;
      id.value = data?.id;
      await nextTick();
      if (data) {
        formApi.setValues({
          ...data,
          roleIds: data.roleList?.map((r) => r.id) ?? [],
        });
      }
    }
  },
});

const getDrawerTitle = computed(() =>
  $t('common.edit', [$t('system.user.name')]),
);
</script>

<template>
  <Drawer :title="getDrawerTitle">
    <Form />
  </Drawer>
</template>
