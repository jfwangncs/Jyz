import type { Recordable } from '@vben/types';

import { requestClient } from '#/api/request';

export namespace SystemRoleApi {
  export interface SystemRolePermission {
    id: number;
    [key: string]: any;
  }

  export interface SystemRole {
    [key: string]: any;
    id: string;
    name: string;
    permissions: SystemRolePermission[];
    description?: string;
    status: 0 | 1;
  }
}

/**
 * 获取角色列表数据
 */
async function getRoleList(params: Recordable<any>) {
  return requestClient.get<Array<SystemRoleApi.SystemRole>>('/role', {
    params,
  });
}

/**
 * 创建角色
 * @param data 角色数据
 */
async function createRole(data: Omit<SystemRoleApi.SystemRole, 'id'>) {
  return requestClient.post<SystemRoleApi.SystemRole>('/role', data);
}

/**
 * 更新角色
 *
 * @param id 角色 ID
 * @param data 角色数据
 */
async function updateRole(
  id: string,
  data: Omit<SystemRoleApi.SystemRole, 'id'>,
) {
  return requestClient.put(`/role/${id}`, data);
}
/**
 * 获取角色已分配的权限 ID 列表
 */
async function getRolePermissions(id: number) {
  return requestClient.get<number[]>(`/role/${id}/permissions`);
}

/**
 * 为角色分配权限
 */
async function assignRolePermissions(id: number, permissionIds: number[]) {
  return requestClient.put(`/role/${id}/permissions`, { permissionIds });
}

async function getAllRoleList(): Promise<SystemRoleApi.SystemRole[]> {
  const result = await requestClient.get<{
    items: SystemRoleApi.SystemRole[];
  }>('/role', { params: { pageIndex: 1, pageSize: 1000 } });
  return result.items ?? [];
}

export {
  assignRolePermissions,
  createRole,
  getAllRoleList,
  getRoleList,
  getRolePermissions,
  updateRole,
};
