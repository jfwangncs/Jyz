import { describe, expect, it, vi } from 'vitest';

import { useColumns, useFormSchema, useGridFormSchema } from '../data';

vi.mock('#/locales', () => ({ $t: (key: string) => key }));

describe('role/data', () => {
  it('useFormSchema marks name as required', () => {
    const schema = useFormSchema();
    const nameField = schema.find((s) => s.fieldName === 'name');
    expect(nameField?.rules).toBe('required');
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

  it('formats createdTime and tolerates empty values', () => {
    const cols = (useColumns(vi.fn()) ?? []) as any[];
    const dateCol = cols.find((c) => c.field === 'createdTime');
    expect(
      typeof dateCol.formatter({ cellValue: '2025-01-01T00:00:00Z' }),
    ).toBe('string');
    expect(dateCol.formatter({ cellValue: null })).toBe('');
  });
});
