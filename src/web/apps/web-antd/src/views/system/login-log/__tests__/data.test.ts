import { describe, expect, it, vi } from 'vitest';

import { useColumns, useGridFormSchema } from '../data';

vi.mock('#/locales', () => ({ $t: (key: string) => key }));

describe('login-log/data', () => {
  it('useGridFormSchema returns a non-empty schema', () => {
    const schema = useGridFormSchema();
    expect(schema.length).toBeGreaterThan(0);
  });

  it('createdTime formatter handles values and empties', () => {
    const cols = (useColumns() ?? []) as any[];
    const dateCol = cols.find((c) => c.field === 'createdTime');
    expect(dateCol).toBeDefined();
    expect(
      typeof dateCol.formatter({ cellValue: '2025-01-01T00:00:00Z' }),
    ).toBe('string');
    expect(dateCol.formatter({ cellValue: null })).toBe('');
  });

  it('result formatter maps 0 to success and other to failed', () => {
    const cols = (useColumns() ?? []) as any[];
    const resultCol = cols.find((c) => c.field === 'result');
    expect(resultCol.formatter({ cellValue: 0 })).toContain('success');
    expect(resultCol.formatter({ cellValue: 1 })).toContain('failed');
  });
});
