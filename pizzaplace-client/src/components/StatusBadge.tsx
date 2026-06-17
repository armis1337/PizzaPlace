import type { OrderStatus } from '../types';

const config: Record<OrderStatus, { label: string; className: string }> = {
  Received:       { label: 'Received',        className: 'badge badge-received' },
  Preparing:      { label: 'Preparing',       className: 'badge badge-preparing' },
  Ready:          { label: 'Ready',           className: 'badge badge-ready' },
  OutForDelivery: { label: 'Out for Delivery', className: 'badge badge-delivery' },
  Delivered:      { label: 'Delivered',       className: 'badge badge-delivered' },
  Cancelled:      { label: 'Cancelled',       className: 'badge badge-cancelled' },
};

export function StatusBadge({ status }: { status: OrderStatus }) {
  const { label, className } = config[status];
  return <span className={className}>{label}</span>;
}
