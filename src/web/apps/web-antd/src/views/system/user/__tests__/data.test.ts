import { describe, expect, it, vi } from 'vitest';

import { useColumns, useFormSchema, useGridFormSchema } from '../data';

vi.mock('#/locales', () => ({ $t: (key: string) => key }));

describe('user/data', () => {
  it('useFormSchema includes profile fields', () => {
    const schema = useFormSchema();
    expect(schema.some((s) => s.fieldName === 'nickName')).toBe(true);
  });

  it('useGridFormSchema returns search filters', () => {
    expect(useGridFormSchema().length).toBeGreaterThan(0);
  });

  it('useColumns wires CellTag when no status callback is provided', () => {
    const cols = (useColumns(vi.fn()) ?? []) as any[];
    const statusCol = cols.find((c) => c.field === 'status');
    expect(statusCol.cellRender.name).toBe('CellTag');
  });

  it('useColumns wires CellSwitch when a status callback is provided', () => {
    const onStatusChange = vi.fn();
    const cols = (useColumns(vi.fn(), onStatusChange) ?? []) as any[];
    const statusCol = cols.find((c) => c.field === 'status');
    expect(statusCol.cellRender.name).toBe('CellSwitch');
    expect(statusCol.cellRender.attrs.beforeChange).toBe(onStatusChange);
  });

  it('exercises lastLoginTime and createdTime formatters', () => {
    const cols = (useColumns(vi.fn()) ?? []) as any[];
    for (const field of ['lastLoginTime', 'createdTime']) {
      const col = cols.find((c) => c.field === field);
      expect(col).toBeDefined();
      expect(typeof col.formatter({ cellValue: '2025-01-01T00:00:00Z' })).toBe(
        'string',
      );
      expect(col.formatter({ cellValue: null })).toBe('');
    }
  });

  it('gender formatter maps 0/1/2 to labels and other to empty', () => {
    const cols = (useColumns(vi.fn()) ?? []) as any[];
    const genderCol = cols.find((c) => c.field === 'gender');
    expect(genderCol.formatter({ cellValue: 0 })).toBe('system.user.unknown');
    expect(genderCol.formatter({ cellValue: 1 })).toBe('system.user.male');
    expect(genderCol.formatter({ cellValue: 2 })).toBe('system.user.female');
    expect(genderCol.formatter({ cellValue: 99 })).toBe('');
  });
});
