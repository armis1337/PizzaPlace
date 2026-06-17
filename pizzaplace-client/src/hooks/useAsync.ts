import { useState, useEffect, useCallback, useRef } from 'react';
import { extractApiError } from '../api/errors';

export interface AsyncState<T> {
  data: T | undefined;
  loading: boolean;
  error: string | null;
  reload: () => void;
}

/**
 * Wraps the "fetch on mount → loading / error / data" pattern.
 * `fn` is read via ref so it never needs to be in `deps`.
 * Pass `deps = []` for a one-time mount fetch.
 */
export function useAsync<T>(fn: () => Promise<T>, deps: readonly unknown[]): AsyncState<T> {
  const [data, setData] = useState<T | undefined>(undefined);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fnRef = useRef(fn);
  useEffect(() => { fnRef.current = fn; });

  const execute = useCallback(() => {
    setLoading(true);
    setError(null);
    fnRef.current()
      .then(result => {
        setData(result);
        setLoading(false);
      })
      .catch(err => {
        setError(extractApiError(err, 'Failed to load data'));
        setLoading(false);
      });
  // deps control when the fetch re-runs, not fn (which is stable via ref)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => { execute(); }, [execute]);

  return { data, loading, error, reload: execute };
}
