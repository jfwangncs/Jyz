<script lang="ts" setup>
import { ref } from 'vue';

import { useVbenDrawer, useVbenForm } from '@vben/common-ui';

import { createDictType, updateDictType } from '#/api';
import { $t } from '#/locales';

import { useDictTypeFormSchema } from '../data';

const emits = defineEmits<{ success: [] }>();
const id = ref<number>();
const isEdit = ref(false);

const [Form, formApi] = useVbenForm({
  schema: useDictTypeFormSchema(),
  showDefaultActions: false,
});

const [Drawer, drawerApi] = useVbenDrawer({
  async onConfirm() {
    const { valid } = await formApi.validate();
    if (!valid) return;
    const values = await formApi.getValues();
    drawerApi.lock();
    const request = isEdit.value
      ? updateDictType(id.value!, {
          name: values.name,
          description: values.description,
        })
      : createDictType({
          code: values.code,
          name: values.name,
          description: values.description,
        });
    request
      .then(() => {
        emits('success');
        drawerApi.close();
      })
      .catch(() => drawerApi.unlock());
  },
  async onOpenChange(isOpen) {
    if (!isOpen) return;
    formApi.resetForm();
    const data = drawerApi.getData<any>();
    id.value = data?.id;
    isEdit.value = !!data?.id;
    if (data) {
      await formApi.setValues(data);
      // code is not editable after creation
      formApi.updateSchema([
        { fieldName: 'code', componentProps: { disabled: isEdit.value } },
      ]);
    } else {
      formApi.updateSchema([
        { fieldName: 'code', componentProps: { disabled: false } },
      ]);
    }
    drawerApi.setState({
      title: isEdit.value
        ? $t('system.dict.editType')
        : $t('system.dict.addType'),
    });
  },
});
</script>

<template>
  <Drawer>
    <Form />
  </Drawer>
</template>
