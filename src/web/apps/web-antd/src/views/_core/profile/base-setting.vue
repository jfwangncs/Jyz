<script setup lang="ts">
import type { Recordable } from '@vben/types';

import type { VbenFormSchema } from '#/adapter/form';

import { computed, onMounted, ref } from 'vue';

import { ProfileBaseSetting } from '@vben/common-ui';

import { message } from 'ant-design-vue';

import { getUserInfoApi } from '#/api';
import { getAllRoleList } from '#/api/system/role';
import { updateProfile } from '#/api/system/user';
import { $t } from '#/locales';

const profileBaseSettingRef = ref();
const submitting = ref(false);
const rolesOptions = ref<{ label: string; value: number }[]>([]);

const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      fieldName: 'userName',
      component: 'Input',
      label: $t('system.user.userName'),
      disabled: true,
    },
    {
      fieldName: 'realName',
      component: 'Input',
      label: $t('system.user.realName'),
    },
    {
      fieldName: 'nickName',
      component: 'Input',
      label: $t('system.user.nickName'),
    },
    {
      fieldName: 'phone',
      component: 'Input',
      label: $t('system.user.phone'),
    },
    {
      fieldName: 'avatar',
      component: 'Input',
      label: $t('system.user.avatar'),
    },
    {
      fieldName: 'gender',
      component: 'Select',
      componentProps: {
        allowClear: true,
        options: [
          { label: $t('system.user.unknown'), value: 0 },
          { label: $t('system.user.male'), value: 1 },
          { label: $t('system.user.female'), value: 2 },
        ],
      },
      label: $t('system.user.gender'),
    },
    {
      fieldName: 'province',
      component: 'Input',
      label: $t('system.user.province'),
    },
    {
      fieldName: 'city',
      component: 'Input',
      label: $t('system.user.city'),
    },
    {
      fieldName: 'country',
      component: 'Input',
      label: $t('system.user.country'),
    },
    {
      fieldName: 'roleList',
      component: 'Select',
      componentProps: {
        mode: 'multiple',
        options: rolesOptions.value,
      },
      label: $t('system.user.roles'),
      disabled: true,
    },
  ];
});

async function handleSubmit(values: Recordable<any>) {
  if (submitting.value) return;
  submitting.value = true;
  try {
    await updateProfile({
      realName: values.realName,
      nickName: values.nickName,
      phone: values.phone,
      avatar: values.avatar,
      gender: values.gender,
      province: values.province,
      city: values.city,
      country: values.country,
    });
    message.success('个人信息已更新');
  } finally {
    submitting.value = false;
  }
}

onMounted(async () => {
  const [userInfo, roles] = await Promise.all([
    getUserInfoApi(),
    getAllRoleList(),
  ]);
  const info = userInfo as any;
  rolesOptions.value = roles.map((r) => ({
    label: r.name,
    value: Number(r.id),
  }));
  profileBaseSettingRef.value.getFormApi().setValues({
    ...info,
    roleList: info.roleList?.map((r: any) => r.id) ?? [],
  });
});
</script>
<template>
  <ProfileBaseSetting
    ref="profileBaseSettingRef"
    :form-schema="formSchema"
    :loading="submitting"
    @submit="handleSubmit"
  />
</template>
