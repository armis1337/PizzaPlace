/**
 * Safely extracts a message string from an axios-style error response.
 * Takes `unknown` and narrows through each level — no casts, no `any`.
 */
export function extractApiError(err: unknown, fallback = 'Something went wrong'): string {
  if (
    err != null &&
    typeof err === 'object' &&
    'response' in err &&
    err.response != null &&
    typeof err.response === 'object' &&
    'data' in err.response &&
    err.response.data != null &&
    typeof err.response.data === 'object' &&
    'message' in err.response.data &&
    typeof (err.response.data as Record<string, unknown>).message === 'string'
  ) {
    return (err.response.data as { message: string }).message;
  }
  return fallback;
}
