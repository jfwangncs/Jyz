import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  createDictItem,
  createDictType,
  getDictItemList,
  getDictTypeList,
  getDictTypeLookup,
  updateDictItem,
  updateDictType,
} from '#/api/system/dict';

// `vi.mock` calls are hoisted to the top of the file, so the mock factory
// cannot reference outer variables directly. Use `vi.hoisted` to share the
// mock functions between the factory and the tests.
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

describe('systemDict API', () => {
  it('getDictTypeList passes pagination params to GET /DictType', async () => {
    get.mockResolvedValue({ items: [], total: 0 });

    await getDictTypeList({ pageIndex: 2, pageSize: 20, searchKey: 'foo' });

    expect(get).toHaveBeenCalledWith('/DictType', {
      params: { pageIndex: 2, pageSize: 20, searchKey: 'foo' },
    });
  });

  it('getDictTypeLookup hits GET /DictType/lookup', async () => {
    get.mockResolvedValue([]);
    await getDictTypeLookup();
    expect(get).toHaveBeenCalledWith('/DictType/lookup');
  });

  it('createDictType posts the payload to /DictType', async () => {
    post.mockResolvedValue({});
    const payload = { code: 'COLOR', description: 'Colors', name: 'Color' };

    await createDictType(payload);

    expect(post).toHaveBeenCalledWith('/DictType', payload);
  });

  it('updateDictType puts to /DictType/{id} (URL contains id)', async () => {
    put.mockResolvedValue({});
    await updateDictType(42, { name: 'Renamed', status: 0 });

    expect(put).toHaveBeenCalledWith('/DictType/42', {
      name: 'Renamed',
      status: 0,
    });
  });

  it('getDictItemList passes params to GET /DictItem', async () => {
    get.mockResolvedValue({ items: [], total: 0 });
    await getDictItemList({ dictTypeId: 1, pageIndex: 1, pageSize: 10 });

    expect(get).toHaveBeenCalledWith('/DictItem', {
      params: { dictTypeId: 1, pageIndex: 1, pageSize: 10 },
    });
  });

  it('createDictItem posts to /DictItem', async () => {
    post.mockResolvedValue({});
    await createDictItem({ code: 'RED', dictTypeId: 1, label: 'Red', sort: 1 });

    expect(post).toHaveBeenCalledWith('/DictItem', {
      code: 'RED',
      dictTypeId: 1,
      label: 'Red',
      sort: 1,
    });
  });

  it('updateDictItem puts to /DictItem/{id}', async () => {
    put.mockResolvedValue({});
    await updateDictItem(7, { label: 'Crimson' });
    expect(put).toHaveBeenCalledWith('/DictItem/7', { label: 'Crimson' });
  });
});
