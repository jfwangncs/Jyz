import { describe, expect, it, vi } from 'vitest';

import {
  useDictItemColumns,
  useDictItemFormSchema,
  useDictTypeColumns,
  useDictTypeFormSchema,
  useGridFormSchema,
} from '../data';

vi.mock('#/locales', () => ({ $t: (key: string) => key }));
vi.mock('@vben/access', () => ({
  useAccess: () => ({ hasAccessByCodes: () => true }),
}));

describe('dict/data', () => {
  it('useDictTypeFormSchema requires a code field', () => {
    const schema = useDictTypeFormSchema();
    const codeField = schema.find((s) => s.fieldName === 'code');
    expect(codeField?.rules).toBe('required');
  });

  it('useDictItemFormSchema and useGridFormSchema are non-empty', () => {
    expect(useDictItemFormSchema().length).toBeGreaterThan(0);
    expect(useGridFormSchema().length).toBeGreaterThan(0);
  });

  it('useDictTypeColumns toggles between CellTag and CellSwitch by callback presence', () => {
    const noCb = (useDictTypeColumns(vi.fn()) ?? []) as any[];
    const withCb = (useDictTypeColumns(vi.fn(), vi.fn()) ?? []) as any[];
    expect(noCb.find((c) => c.field === 'status').cellRender.name).toBe(
      'CellTag',
    );
    expect(withCb.find((c) => c.field === 'status').cellRender.name).toBe(
      'CellSwitch',
    );
  });

  it('useDictItemColumns toggles between CellTag and CellSwitch by callback presence', () => {
    const noCb = (useDictItemColumns(vi.fn()) ?? []) as any[];
    const withCb = (useDictItemColumns(vi.fn(), vi.fn()) ?? []) as any[];
    expect(noCb.find((c) => c.field === 'status').cellRender.name).toBe(
      'CellTag',
    );
    expect(withCb.find((c) => c.field === 'status').cellRender.name).toBe(
      'CellSwitch',
    );
  });

  it('exercises date formatters and operation `show` predicates', () => {
    const cols = (useDictTypeColumns(vi.fn()) ?? []) as any[];
    const dateCol = cols.find((c) => typeof c.formatter === 'function');
    if (dateCol) {
      expect(
        typeof dateCol.formatter({ cellValue: '2025-01-01T00:00:00Z' }),
      ).toBe('string');
      expect(dateCol.formatter({ cellValue: null })).toBe('');
    }
    const opCol = cols.find((c) => c.cellRender?.name === 'CellOperation');
    if (opCol) {
      const opt = opCol.cellRender.options.find(
        (o: any) => typeof o.show === 'function',
      );
      if (opt) expect(opt.show()).toBe(true);
    }
  });
});
