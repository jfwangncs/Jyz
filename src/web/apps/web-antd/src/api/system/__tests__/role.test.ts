import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  assignRolePermissions,
  createRole,
  getAllRoleList,
  getRoleList,
  getRolePermissions,
  updateRole,
} from '#/api/system/role';

const { get, post, put } = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
}));

vi.mock('#/api/request', () => ({
  requestClient: { get, post, put },
}));

beforeEach(() => {
  get.mockReset();
  post.mockReset();
  put.mockReset();
});

describe('systemRole API', () => {
  it('getRoleList sends GET /role with params', async () => {
    get.mockResolvedValue([]);
    await getRoleList({ pageIndex: 1, pageSize: 10 });
    expect(get).toHaveBeenCalledWith('/role', {
      params: { pageIndex: 1, pageSize: 10 },
    });
  });

  it('createRole sends POST /role with payload', async () => {
    post.mockResolvedValue({});
    const data = {
      name: 'admin',
      permissions: [],
      status: 1 as const,
    };
    await createRole(data);
    expect(post).toHaveBeenCalledWith('/role', data);
  });

  it('updateRole sends PUT /role/:id with payload', async () => {
    put.mockResolvedValue({});
    const data = {
      name: 'editor',
      permissions: [],
      status: 0 as const,
    };
    await updateRole('42', data);
    expect(put).toHaveBeenCalledWith('/role/42', data);
  });

  it('getRolePermissions sends GET /role/:id/permissions', async () => {
    get.mockResolvedValue([1, 2, 3]);
    const result = await getRolePermissions(5);
    expect(get).toHaveBeenCalledWith('/role/5/permissions');
    expect(result).toEqual([1, 2, 3]);
  });

  it('assignRolePermissions sends PUT /role/:id/permissions with permissionIds', async () => {
    put.mockResolvedValue({});
    await assignRolePermissions(5, [1, 2, 3]);
    expect(put).toHaveBeenCalledWith('/role/5/permissions', {
      permissionIds: [1, 2, 3],
    });
  });

  it('getAllRoleList requests page 1 size 1000 and returns items', async () => {
    const items = [{ id: '1', name: 'a', permissions: [], status: 1 }];
    get.mockResolvedValue({ items });
    const result = await getAllRoleList();
    expect(get).toHaveBeenCalledWith('/role', {
      params: { pageIndex: 1, pageSize: 1000 },
    });
    expect(result).toBe(items);
  });

  it('getAllRoleList returns [] when items is missing', async () => {
    get.mockResolvedValue({});
    const result = await getAllRoleList();
    expect(result).toEqual([]);
  });
});
