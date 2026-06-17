export function LoadingState({ message = 'Loading…' }: { message?: string }) {
  return <p className="async-loading">{message}</p>;
}
