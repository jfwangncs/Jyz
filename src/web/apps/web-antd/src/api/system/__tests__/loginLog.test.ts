import { beforeEach, describe, expect, it, vi } from 'vitest';

import { getLoginLogList } from '#/api/system/loginLog';

const { get } = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock('#/api/request', () => ({
  requestClient: { get },
}));

beforeEach(() => {
  get.mockReset();
});

describe('systemLoginLog API', () => {
  it('getLoginLogList sends GET /LoginLog with params', async () => {
    get.mockResolvedValue({ items: [], total: 0 });
    await getLoginLogList({ pageIndex: 1, pageSize: 10, result: 1 });
    expect(get).toHaveBeenCalledWith('/LoginLog', {
      params: { pageIndex: 1, pageSize: 10, result: 1 },
    });
  });
});
