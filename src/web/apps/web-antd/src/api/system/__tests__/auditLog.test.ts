import { beforeEach, describe, expect, it, vi } from 'vitest';

import { getAuditLogList } from '#/api/system/auditLog';

const { get } = vi.hoisted(() => ({ get: vi.fn() }));

vi.mock('#/api/request', () => ({
  requestClient: { get },
}));

beforeEach(() => {
  get.mockReset();
});

describe('systemAuditLog API', () => {
  it('getAuditLogList sends GET /AuditLog with params', async () => {
    get.mockResolvedValue({ items: [], total: 0 });
    await getAuditLogList({ pageIndex: 2, pageSize: 20, action: 'Added' });
    expect(get).toHaveBeenCalledWith('/AuditLog', {
      params: { pageIndex: 2, pageSize: 20, action: 'Added' },
    });
  });
});
