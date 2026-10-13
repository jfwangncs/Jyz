import { requestClient } from '#/api/request';

export namespace SystemLoginLogApi {
  export interface LoginLog {
    id: number;
    userName: null | string;
    ip: null | string;
    userAgent: null | string;
    platform: number;
    result: number;
    failReason: null | string;
    createdTime: string;
  }
}

export async function getLoginLogList(params: Record<string, any>) {
  return requestClient.get<{
    items: SystemLoginLogApi.LoginLog[];
    total: number;
  }>('/LoginLog', { params });
}
