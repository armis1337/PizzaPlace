import { describe, it, expect } from 'vitest';
import { extractApiError } from './errors';

describe('extractApiError', () => {
  it('returns the message from a well-formed axios error', () => {
    const err = { response: { data: { message: 'Pizza not found' } } };
    expect(extractApiError(err)).toBe('Pizza not found');
  });

  it('returns a custom fallback when one is provided', () => {
    expect(extractApiError(new Error('network'), 'Custom fallback')).toBe('Custom fallback');
  });

  it('returns the default fallback when no response is present', () => {
    expect(extractApiError(new Error('network'))).toBe('Something went wrong');
  });

  it('handles null without throwing', () => {
    expect(() => extractApiError(null)).not.toThrow();
    expect(extractApiError(null)).toBe('Something went wrong');
  });

  it('handles undefined without throwing', () => {
    expect(() => extractApiError(undefined)).not.toThrow();
    expect(extractApiError(undefined)).toBe('Something went wrong');
  });

  it('handles a plain string without throwing', () => {
    expect(extractApiError('oops')).toBe('Something went wrong');
  });

  it('handles a response with no data', () => {
    expect(extractApiError({ response: {} })).toBe('Something went wrong');
  });

  it('handles a non-string message field', () => {
    const err = { response: { data: { message: 42 } } };
    expect(extractApiError(err)).toBe('Something went wrong');
  });
});
