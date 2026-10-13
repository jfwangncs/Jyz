import type { UserInfo } from '@vben/types';

import { requestClient } from '#/api/request';

/**
 * 获取用户信息
 */
export async function getUserInfoApi() {
  return requestClient.get<UserInfo>('/user/info');
}

/**
 * 修改密码
 */
export async function changePasswordApi(data: {
  newPassword: string;
  oldPassword: string;
}) {
  return requestClient.put('/user/change-password', data);
}
