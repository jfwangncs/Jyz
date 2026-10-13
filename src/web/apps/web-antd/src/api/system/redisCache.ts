import { requestClient } from '#/api/request';

export namespace SystemRedisCacheApi {
  export interface RedisCacheItem {
    key: string;
    ttl: number; // -1 = no expiry, >0 = seconds remaining
    value: null | string;
    isJson: boolean;
  }
}

export async function getRedisCacheList() {
  return requestClient.get<SystemRedisCacheApi.RedisCacheItem[]>('/RedisCache');
}

export async function deleteRedisCacheKeys(keys: string[]) {
  return requestClient.delete('/RedisCache', { data: { keys } });
}
