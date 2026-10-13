<script setup lang="ts">
import type { VbenFormSchema } from '#/adapter/form';

import { computed, ref } from 'vue';

import { ProfilePasswordSetting, z } from '@vben/common-ui';

import { message } from 'ant-design-vue';

import { changePasswordApi } from '#/api/core/user';
import { $t } from '#/locales';
import { useAuthStore } from '#/store';

const authStore = useAuthStore();
const submitting = ref(false);

const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      fieldName: 'oldPassword',
      label: $t('page.profile.password.oldPasswordLabel'),
      component: 'VbenInputPassword',
      componentProps: {
        placeholder: $t('page.profile.password.oldPasswordPlaceholder'),
      },
      rules: z
        .string({
          required_error: $t('page.profile.password.oldPasswordRequired'),
        })
        .min(1, { message: $t('page.profile.password.oldPasswordRequired') }),
    },
    {
      fieldName: 'newPassword',
      label: $t('page.profile.password.newPasswordLabel'),
      component: 'VbenInputPassword',
      componentProps: {
        passwordStrength: true,
        placeholder: $t('page.profile.password.newPasswordPlaceholder'),
      },
      rules: z
        .string({
          required_error: $t('page.profile.password.newPasswordRequired'),
        })
        .min(6, { message: $t('page.profile.password.newPasswordMin') }),
    },
    {
      fieldName: 'confirmPassword',
      label: $t('page.profile.password.confirmPasswordLabel'),
      component: 'VbenInputPassword',
      componentProps: {
        passwordStrength: true,
        placeholder: $t('page.profile.password.confirmPasswordPlaceholder'),
      },
      dependencies: {
        rules(values) {
          const { newPassword } = values;
          return z
            .string({
              required_error: $t(
                'page.profile.password.confirmPasswordRequired',
              ),
            })
            .min(1, {
              message: $t('page.profile.password.confirmPasswordRequired'),
            })
            .refine((value) => value === newPassword, {
              message: $t('page.profile.password.confirmPasswordMismatch'),
            });
        },
        triggerFields: ['newPassword'],
      },
    },
  ];
});

async function handleSubmit(values: Record<string, any>) {
  if (submitting.value) return;
  submitting.value = true;
  try {
    await changePasswordApi({
      oldPassword: values.oldPassword,
      newPassword: values.newPassword,
    });
    message.success($t('page.profile.password.changeSuccess'));
    await authStore.logout(false);
  } finally {
    submitting.value = false;
  }
}
</script>
<template>
  <ProfilePasswordSetting
    class="w-1/3"
    :form-schema="formSchema"
    :loading="submitting"
    @submit="handleSubmit"
  />
</template>
