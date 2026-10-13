import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  createPermission,
  deletePermission,
  getPermissionById,
  getPermissionList,
  getPermissionTreeList,
  syncPermissions,
  updatePermission,
} from '#/api/system/permission';

const {
  delete: del,
  get,
  post,
  put,
} = vi.hoisted(() => ({
  delete: vi.fn(),
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
}));

vi.mock('#/api/request', () => ({
  requestClient: { delete: del, get, post, put },
}));

beforeEach(() => {
  del.mockReset();
  get.mockReset();
  post.mockReset();
  put.mockReset();
});

describe('systemPermission API', () => {
  it('getPermissionList sends GET /permission with params', async () => {
    get.mockResolvedValue({ items: [], total: 0 });
    await getPermissionList({ searchKey: 'user' });
    expect(get).toHaveBeenCalledWith('/permission', {
      params: { searchKey: 'user' },
    });
  });

  it('getPermissionTreeList sends GET /permission/list', async () => {
    get.mockResolvedValue([]);
    await getPermissionTreeList();
    expect(get).toHaveBeenCalledWith('/permission/list');
  });

  it('getPermissionById sends GET /permission/:id', async () => {
    get.mockResolvedValue({ id: 7 });
    await getPermissionById(7);
    expect(get).toHaveBeenCalledWith('/permission/7');
  });

  it('createPermission sends POST /permission with payload', async () => {
    post.mockResolvedValue({});
    const data = { name: 'M', code: 'm:a', type: 1 };
    await createPermission(data);
    expect(post).toHaveBeenCalledWith('/permission', data);
  });

  it('updatePermission sends PUT /permission/:id with payload', async () => {
    put.mockResolvedValue({});
    await updatePermission(3, { name: 'N' });
    expect(put).toHaveBeenCalledWith('/permission/3', { name: 'N' });
  });

  it('deletePermission sends DELETE /permission/:id', async () => {
    del.mockResolvedValue({});
    await deletePermission(9);
    expect(del).toHaveBeenCalledWith('/permission/9');
  });

  it('syncPermissions sends POST /permission/sync', async () => {
    post.mockResolvedValue({});
    await syncPermissions();
    expect(post).toHaveBeenCalledWith('/permission/sync');
  });
});
