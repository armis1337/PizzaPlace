interface Props {
  message: string;
  onRetry: () => void;
}

export function ErrorState({ message, onRetry }: Props) {
  return (
    <div className="async-error">
      <p className="async-error-msg">{message}</p>
      <button className="btn btn-ghost" onClick={onRetry}>Retry</button>
    </div>
  );
}
