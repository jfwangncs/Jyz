import type { Recordable } from '@vben/types';

import { requestClient } from '#/api/request';

export namespace SystemUserApi {
  export interface UserRoleItem {
    id: number;
    name: string;
  }

  export interface SystemUser {
    [key: string]: any;
    id: number;
    userName: string;
    nickName?: string;
    realName?: string;
    phone?: string;
    avatar?: string;
    gender?: number;
    province?: string;
    city?: string;
    country?: string;
    lastLoginTime?: string;
    createdTime?: string;
    status: 0 | 1;
    roles?: string[];
    roleList?: UserRoleItem[];
  }
}

async function getUserList(params: Recordable<any>) {
  return requestClient.get<Array<SystemUserApi.SystemUser>>('/user', {
    params,
  });
}

async function updateUser(id: number, data: Recordable<any>) {
  return requestClient.put(`/user/${id}`, data);
}

async function updateProfile(data: Recordable<any>) {
  return requestClient.put('/user/profile', data);
}

export { getUserList, updateProfile, updateUser };
