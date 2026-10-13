import { describe, expect, it, vi } from 'vitest';

import { useColumns, useGridFormSchema } from '../data';

vi.mock('#/locales', () => ({ $t: (key: string) => key }));
vi.mock('@vben/access', () => ({
  useAccess: () => ({ hasAccessByCodes: () => true }),
}));

describe('redis-cache/data', () => {
  it('useGridFormSchema returns a non-empty schema', () => {
    const schema = useGridFormSchema();
    expect(schema.length).toBeGreaterThan(0);
  });

  it('useColumns wires the action callback and exercises action `show` predicate', () => {
    const onClick = vi.fn();
    const cols = (useColumns(onClick) ?? []) as any[];
    expect(cols.length).toBeGreaterThan(0);
    const opCol = cols.find((c) => c.cellRender?.name === 'CellOperation');
    expect(opCol).toBeDefined();
    expect(opCol.cellRender.attrs.onClick).toBe(onClick);
    const deleteOpt = opCol.cellRender.options.find(
      (o: any) => o.code === 'delete',
    );
    expect(deleteOpt.show()).toBe(true);
  });
});
