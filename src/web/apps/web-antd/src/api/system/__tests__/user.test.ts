import { beforeEach, describe, expect, it, vi } from 'vitest';

import { getUserList, updateProfile, updateUser } from '#/api/system/user';

const { get, put } = vi.hoisted(() => ({
  get: vi.fn(),
  put: vi.fn(),
}));

vi.mock('#/api/request', () => ({
  requestClient: { get, put },
}));

beforeEach(() => {
  get.mockReset();
  put.mockReset();
});

describe('systemUser API', () => {
  it('getUserList sends GET /user with params', async () => {
    get.mockResolvedValue([]);
    await getUserList({ searchKey: 'admin' });
    expect(get).toHaveBeenCalledWith('/user', {
      params: { searchKey: 'admin' },
    });
  });

  it('updateUser sends PUT /user/:id with payload', async () => {
    put.mockResolvedValue({});
    await updateUser(11, { nickName: 'Alice' });
    expect(put).toHaveBeenCalledWith('/user/11', { nickName: 'Alice' });
  });

  it('updateProfile sends PUT /user/profile with payload', async () => {
    put.mockResolvedValue({});
    await updateProfile({ phone: '123' });
    expect(put).toHaveBeenCalledWith('/user/profile', { phone: '123' });
  });
});
