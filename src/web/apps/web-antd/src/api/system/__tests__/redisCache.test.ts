import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  deleteRedisCacheKeys,
  getRedisCacheList,
} from '#/api/system/redisCache';

const { delete: del, get } = vi.hoisted(() => ({
  delete: vi.fn(),
  get: vi.fn(),
}));

vi.mock('#/api/request', () => ({
  requestClient: { delete: del, get },
}));

beforeEach(() => {
  del.mockReset();
  get.mockReset();
});

describe('systemRedisCache API', () => {
  it('getRedisCacheList sends GET /RedisCache', async () => {
    get.mockResolvedValue([]);
    await getRedisCacheList();
    expect(get).toHaveBeenCalledWith('/RedisCache');
  });

  it('deleteRedisCacheKeys sends DELETE /RedisCache with keys body', async () => {
    del.mockResolvedValue(undefined);
    await deleteRedisCacheKeys(['a', 'b', 'c']);
    expect(del).toHaveBeenCalledWith('/RedisCache', {
      data: { keys: ['a', 'b', 'c'] },
    });
  });
});
