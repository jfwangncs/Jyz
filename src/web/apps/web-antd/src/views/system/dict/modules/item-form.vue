<script lang="ts" setup>
import { ref } from 'vue';

import { useVbenDrawer, useVbenForm } from '@vben/common-ui';

import { createDictItem, updateDictItem } from '#/api';
import { $t } from '#/locales';

import { useDictItemFormSchema } from '../data';

const emits = defineEmits<{ success: [] }>();
const id = ref<number>();
const dictTypeId = ref<number>();
const isEdit = ref(false);

const [Form, formApi] = useVbenForm({
  schema: useDictItemFormSchema(),
  showDefaultActions: false,
});

const [Drawer, drawerApi] = useVbenDrawer({
  async onConfirm() {
    const { valid } = await formApi.validate();
    if (!valid) return;
    const values = await formApi.getValues();
    drawerApi.lock();
    const request = isEdit.value
      ? updateDictItem(id.value!, { label: values.label, sort: values.sort })
      : createDictItem({
          dictTypeId: dictTypeId.value!,
          code: values.code,
          label: values.label,
          sort: values.sort,
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
    dictTypeId.value = data?.dictTypeId;
    isEdit.value = !!data?.id;
    if (data?.id) {
      await formApi.setValues(data);
      formApi.updateSchema([
        { fieldName: 'code', componentProps: { disabled: true } },
      ]);
    } else {
      formApi.updateSchema([
        { fieldName: 'code', componentProps: { disabled: false } },
      ]);
    }
    drawerApi.setState({
      title: isEdit.value
        ? $t('system.dict.editItem')
        : $t('system.dict.addItem'),
    });
  },
});
</script>

<template>
  <Drawer>
    <Form />
  </Drawer>
</template>
