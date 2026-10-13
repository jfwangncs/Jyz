import { requestClient } from '#/api/request';

export namespace SystemAuditLogApi {
  export interface AuditLog {
    id: number;
    userId: null | number;
    userName: null | string;
    action: string;
    resource: string;
    resourceId: null | string;
    oldValue: null | string;
    newValue: null | string;
    ip: null | string;
    createdTime: string;
  }
}

export async function getAuditLogList(params: Record<string, any>) {
  return requestClient.get<{
    items: SystemAuditLogApi.AuditLog[];
    total: number;
  }>('/AuditLog', { params });
}
