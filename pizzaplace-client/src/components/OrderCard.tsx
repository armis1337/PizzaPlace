import type { Order } from '../types';
import { StatusBadge } from './StatusBadge';

interface Props {
  order: Order;
  actions?: React.ReactNode;
  /** Optional informational row shown below the actions (e.g. a fulfillment constraint). */
  note?: React.ReactNode;
}

export function OrderCard({ order, actions, note }: Props) {
  const time = new Date(order.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

  return (
    <div className="order-card">
      <div className="order-card-header">
        <div>
          <span className="order-id">#{order.id}</span>
          <span className="order-name">{order.customerName}</span>
        </div>
        <div className="order-card-meta">
          <StatusBadge status={order.status} />
          <span className="order-time">{time}</span>
        </div>
      </div>
      <ul className="order-items">
        {order.items.map((item, i) => (
          <li key={i}>
            {item.quantity}× {item.pizzaName}
          </li>
        ))}
      </ul>
      {order.status === 'Cancelled' && (
        <p className="order-cancel-reason">
          Cancelled{order.cancellationReason ? ` — ${order.cancellationReason}` : ''}
        </p>
      )}
      <div className="order-card-footer">
        <span className="order-total">€{order.totalPrice.toFixed(2)}</span>
        {actions && <div className="order-actions">{actions}</div>}
        {note && <div className="order-note">{note}</div>}
      </div>
    </div>
  );
}
