import { requestClient } from '#/api/request';

export namespace SystemDictApi {
  export interface DictItem {
    id: number;
    dictTypeId: number;
    code: string;
    label: string;
    sort: number;
    status: number;
    createdTime: string;
  }

  export interface DictType {
    id: number;
    code: string;
    name: string;
    description?: string;
    status: number;
    createdTime: string;
    items: DictItem[];
  }

  export interface CreateDictTypeParams {
    code: string;
    name: string;
    description?: string;
  }

  export interface UpdateDictTypeParams {
    name?: string;
    description?: string;
    status?: number;
  }

  export interface CreateDictItemParams {
    dictTypeId: number;
    code: string;
    label: string;
    sort?: number;
  }

  export interface UpdateDictItemParams {
    label?: string;
    sort?: number;
    status?: number;
  }
}

export async function getDictTypeList(params: Record<string, any>) {
  return requestClient.get<{
    items: SystemDictApi.DictType[];
    total: number;
  }>('/DictType', { params });
}

export async function getDictTypeLookup() {
  return requestClient.get<SystemDictApi.DictType[]>('/DictType/lookup');
}

export async function createDictType(data: SystemDictApi.CreateDictTypeParams) {
  return requestClient.post('/DictType', data);
}

export async function updateDictType(
  id: number,
  data: SystemDictApi.UpdateDictTypeParams,
) {
  return requestClient.put(`/DictType/${id}`, data);
}

export async function getDictItemList(params: Record<string, any>) {
  return requestClient.get<{
    items: SystemDictApi.DictItem[];
    total: number;
  }>('/DictItem', { params });
}

export async function createDictItem(data: SystemDictApi.CreateDictItemParams) {
  return requestClient.post('/DictItem', data);
}

export async function updateDictItem(
  id: number,
  data: SystemDictApi.UpdateDictItemParams,
) {
  return requestClient.put(`/DictItem/${id}`, data);
}
