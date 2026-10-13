import type { Recordable } from '@vben/types';

import { requestClient } from '#/api/request';

export namespace SystemPermissionApi {
  export interface SystemPermission {
    [key: string]: any;
    id: number;
    name: string;
    code: string;
    type: number;
    description?: string;
    icon?: string;
    sort: number;
    parentId?: number;
    status: number;
    createdTime: string;
    children?: SystemPermission[];
  }

  export interface CreatePermissionParams {
    name: string;
    code: string;
    type: number;
    description?: string;
    icon?: string;
    sort?: number;
    parentId?: number;
  }

  export interface UpdatePermissionParams {
    name?: string;
    description?: string;
    icon?: string;
    sort?: number;
    status?: number;
  }
}

export async function getPermissionList(params: Recordable<any>) {
  return requestClient.get<{
    items: SystemPermissionApi.SystemPermission[];
    total: number;
  }>('/permission', { params });
}

export async function getPermissionTreeList() {
  return requestClient.get<SystemPermissionApi.SystemPermission[]>(
    '/permission/list',
  );
}

export async function getPermissionById(id: number) {
  return requestClient.get<SystemPermissionApi.SystemPermission>(
    `/permission/${id}`,
  );
}

export async function createPermission(
  data: SystemPermissionApi.CreatePermissionParams,
) {
  return requestClient.post('/permission', data);
}

export async function updatePermission(
  id: number,
  data: SystemPermissionApi.UpdatePermissionParams,
) {
  return requestClient.put(`/permission/${id}`, data);
}

export async function deletePermission(id: number) {
  return requestClient.delete(`/permission/${id}`);
}

export async function syncPermissions() {
  return requestClient.post('/permission/sync');
}
