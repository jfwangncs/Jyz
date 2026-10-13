<script lang="ts" setup>
import type { SystemDictApi } from '#/api';

import { onMounted, ref } from 'vue';

import { useAccess } from '@vben/access';
import { useVbenDrawer } from '@vben/common-ui';
import { Plus, RotateCw } from '@vben/icons';

import { Button, Switch, Table, Tag } from 'ant-design-vue';

import { getDictItemList, updateDictItem } from '#/api';
import { $t } from '#/locales';

import ItemForm from './item-form.vue';

const props = defineProps<{ row: SystemDictApi.DictType }>();

const { hasAccessByCodes } = useAccess();
const items = ref<SystemDictApi.DictItem[]>([]);
const loading = ref(false);

async function loadItems() {
  loading.value = true;
  try {
    const res = await getDictItemList({
      dictTypeId: props.row.id,
      pageSize: 100,
    });
    items.value = res.items ?? [];
  } catch (error) {
    console.error('[ItemsPanel] loadItems failed', error);
  } finally {
    loading.value = false;
  }
}

const [ItemFormDrawer, itemFormDrawerApi] = useVbenDrawer({
  connectedComponent: ItemForm,
  destroyOnClose: true,
});

async function onStatusChange(checked: boolean, row: SystemDictApi.DictItem) {
  const newStatus = checked ? 1 : 0;
  await updateDictItem(row.id, { status: newStatus });
  await loadItems();
}

const columns = [
  { title: $t('system.dict.itemCode'), dataIndex: 'code', width: 140 },
  { title: $t('system.dict.label'), dataIndex: 'label' },
  { title: $t('system.dict.sort'), dataIndex: 'sort', width: 80 },
  {
    title: $t('system.dict.status'),
    dataIndex: 'status',
    width: 100,
    key: 'status',
  },
  {
    title: $t('system.dict.operation'),
    dataIndex: 'operation',
    width: 100,
    key: 'operation',
  },
];

onMounted(loadItems);
</script>

<template>
  <ItemFormDrawer @success="loadItems" />
  <div class="p-3">
    <div class="mb-2 flex items-center gap-2">
      <span class="text-sm font-medium text-gray-600">
        {{ $t('system.dict.itemList') }}: {{ row.name }}
      </span>
      <Button
        v-if="hasAccessByCodes(['dict-item:add'])"
        size="small"
        type="primary"
        @click="itemFormDrawerApi.setData({ dictTypeId: row.id }).open()"
      >
        <Plus class="size-3" />
        {{ $t('system.dict.addItem') }}
      </Button>
      <Button size="small" :loading="loading" @click="loadItems">
        <RotateCw class="size-3" />
      </Button>
    </div>
    <Table
      :columns="columns"
      :data-source="items"
      :loading="loading"
      :pagination="false"
      row-key="id"
      size="small"
      bordered
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <Switch
            v-if="hasAccessByCodes(['dict-item:edit'])"
            :checked="record.status === 1"
            :checked-children="$t('common.enabled')"
            :un-checked-children="$t('common.disabled')"
            @change="
              (checked) =>
                onStatusChange(
                  checked as boolean,
                  record as SystemDictApi.DictItem,
                )
            "
          />
          <Tag v-else :color="record.status === 1 ? 'success' : 'error'">
            {{
              record.status === 1 ? $t('common.enabled') : $t('common.disabled')
            }}
          </Tag>
        </template>
        <template v-else-if="column.key === 'operation'">
          <Button
            v-if="hasAccessByCodes(['dict-item:edit'])"
            size="small"
            type="link"
            @click="itemFormDrawerApi.setData(record).open()"
          >
            {{ $t('common.edit') }}
          </Button>
        </template>
      </template>
    </Table>
  </div>
</template>
