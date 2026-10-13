import { describe, expect, it, vi } from 'vitest';

import { useColumns, useGridFormSchema } from '../data';

vi.mock('#/locales', () => ({ $t: (key: string) => key }));

describe('audit-log/data', () => {
  it('useGridFormSchema returns a non-empty schema', () => {
    const schema = useGridFormSchema();
    expect(Array.isArray(schema)).toBe(true);
    expect(schema.length).toBeGreaterThan(0);
  });

  it('useColumns returns column definitions and a date formatter', () => {
    const cols = (useColumns() ?? []) as any[];
    expect(cols.length).toBeGreaterThan(0);
    const dateCol = cols.find((c) => typeof c.formatter === 'function');
    expect(dateCol).toBeDefined();
    expect(
      typeof dateCol.formatter({ cellValue: '2025-01-01T00:00:00Z' }),
    ).toBe('string');
    expect(dateCol.formatter({ cellValue: null })).toBe('');
  });
});
