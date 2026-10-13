import type { SystemPermissionApi } from '#/api/system/permission';

import { describe, expect, it, vi } from 'vitest';

import {
  buildParentTree,
  getPermissionTypeOptions,
  useColumns,
  useFormSchema,
  useGridFormSchema,
} from '../data';

// `data.ts` calls `$t()` from `#/locales` at module load time when building
// schema/column factories. We only exercise the pure tree builder here, so
// stub the locale module to avoid pulling in the whole i18n runtime.
vi.mock('#/locales', () => ({
  $t: (key: string) => key,
}));

type Permission = SystemPermissionApi.SystemPermission;

const make = (over: Partial<Permission>): Permission =>
  ({
    code: '',
    id: 0,
    name: '',
    parentId: null,
    sort: 0,
    status: 1,
    type: 1,
    ...over,
  }) as Permission;

describe('buildParentTree', () => {
  it('returns an empty array when input is empty', () => {
    expect(buildParentTree([])).toEqual([]);
  });

  it('keeps only Menu (type=1) and Directory (type=3) nodes', () => {
    const items: Permission[] = [
      make({ id: 1, name: 'menu', type: 1 }),
      make({ id: 2, name: 'btn', type: 2 }),
      make({ id: 3, name: 'dir', type: 3 }),
      make({ id: 4, name: 'link', type: 4 }),
    ];

    const tree = buildParentTree(items);

    expect(tree.map((n) => n.id)).toEqual([1, 3]);
  });

  it('builds nested children based on parentId', () => {
    const items: Permission[] = [
      make({ id: 1, name: 'root', type: 3 }),
      make({ id: 2, name: 'child', parentId: 1, type: 1 }),
      make({ id: 3, name: 'grand', parentId: 2, type: 1 }),
    ];

    const tree = buildParentTree(items);

    expect(tree).toHaveLength(1);
    expect(tree[0]?.id).toBe(1);
    expect((tree[0] as any).children).toHaveLength(1);
    expect((tree[0] as any).children[0].id).toBe(2);
    expect((tree[0] as any).children[0].children[0].id).toBe(3);
  });

  it('sorts siblings by ascending `sort`, treating undefined as 0', () => {
    const items: Permission[] = [
      make({ id: 1, name: 'b', sort: 5, type: 1 }),
      make({ id: 2, name: 'a', sort: 1, type: 1 }),
      make({ id: 3, name: 'c', type: 1 }), // sort undefined → 0
    ];

    const tree = buildParentTree(items);

    expect(tree.map((n) => n.id)).toEqual([3, 2, 1]);
  });

  it('excludes the given id from the tree (used when editing a permission)', () => {
    const items: Permission[] = [
      make({ id: 1, name: 'self', type: 3 }),
      make({ id: 2, name: 'other', type: 1 }),
    ];

    const tree = buildParentTree(items, 1);

    expect(tree.map((n) => n.id)).toEqual([2]);
  });

  it('omits `children` for leaf nodes', () => {
    const items: Permission[] = [make({ id: 1, name: 'leaf', type: 1 })];

    const tree = buildParentTree(items);

    expect(tree[0]).not.toHaveProperty('children');
  });
});

describe('getPermissionTypeOptions', () => {
  it('returns all four permission types with stable values', () => {
    const options = getPermissionTypeOptions();
    expect(options.map((o) => o.value)).toEqual([1, 2, 3, 4]);
    // colors should be defined for visual distinction
    expect(options.every((o) => o.color)).toBe(true);
  });
});

describe('schema and column factories', () => {
  it('useFormSchema includes type/name/code with required validators', () => {
    const schema = useFormSchema();
    expect(schema.length).toBeGreaterThan(0);
    expect(schema.find((s) => s.fieldName === 'name')?.rules).toBe('required');
    expect(schema.find((s) => s.fieldName === 'code')?.rules).toBe('required');

    const iconField = schema.find((s) => s.fieldName === 'icon') as any;
    expect(iconField).toBeDefined();
    const showFn = (iconField.dependencies as any).show as (v: any) => boolean;
    expect(showFn({ type: 1 })).toBe(true);
    expect(showFn({ type: 2 })).toBe(false);
  });

  it('useGridFormSchema returns search filters', () => {
    const schema = useGridFormSchema();
    expect(schema.length).toBeGreaterThan(0);
  });

  it('useColumns wires CellTag when no status callback is provided', () => {
    const cols = (useColumns(vi.fn()) ?? []) as any[];
    const statusCol = cols.find((c) => c.field === 'status');
    expect((statusCol as any).cellRender.name).toBe('CellTag');
  });

  it('useColumns wires CellSwitch when a status callback is provided', () => {
    const onStatusChange = vi.fn();
    const cols = (useColumns(vi.fn(), onStatusChange) ?? []) as any[];
    const statusCol = cols.find((c) => c.field === 'status');
    expect((statusCol as any).cellRender.name).toBe('CellSwitch');
    expect((statusCol as any).cellRender.attrs.beforeChange).toBe(
      onStatusChange,
    );
  });
});
