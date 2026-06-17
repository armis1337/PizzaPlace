import { useEffect, useState } from 'react';
import { getDeliveryOrders, claimOrder, deliverOrder } from '../../api/delivery';
import { extractApiError } from '../../api/errors';
import { useAsync } from '../../hooks/useAsync';
import type { Order } from '../../types';
import { OrderCard } from '../../components/OrderCard';
import { LoadingState } from '../../components/LoadingState';
import { ErrorState } from '../../components/ErrorState';
import { useToast, ToastContainer } from '../../components/Toast';
import { useOrderHub } from '../../signalr/useOrderHub';

export function DeliveryBoard({ token, username }: { token: string; username: string }) {
  const { data: initialOrders, loading, error, reload } = useAsync(getDeliveryOrders, []);
  const [orders, setOrders] = useState<Order[]>([]);
  const { toasts, addToast } = useToast();

  useEffect(() => {
    if (initialOrders !== undefined) setOrders(initialOrders);
  }, [initialOrders]);

  useOrderHub('Delivery', {
    OrderReady: (order: unknown) => {
      setOrders(prev => {
        const o = order as Order;
        if (prev.find(x => x.id === o.id)) return prev;
        addToast(`Order #${o.id} is ready for pickup!`, 'info');
        return [...prev, o];
      });
    }
  }, token);

  const handleClaim = async (id: number) => {
    try {
      const updated = await claimOrder(id);
      setOrders(prev => prev.map(o => o.id === id ? updated : o));
      addToast(`Order #${id} claimed — out for delivery!`, 'success');
    } catch (err) {
      addToast(extractApiError(err, 'Failed to claim order'), 'error');
    }
  };

  const handleDeliver = async (id: number) => {
    try {
      await deliverOrder(id);
      setOrders(prev => prev.filter(o => o.id !== id));
      addToast(`Order #${id} delivered!`, 'success');
    } catch (err) {
      addToast(extractApiError(err, 'Failed to mark delivered'), 'error');
    }
  };

  const ready = orders.filter(o => o.status === 'Ready');
  const outForDelivery = orders.filter(o => o.status === 'OutForDelivery');

  return (
    <div className="dashboard">
      <ToastContainer toasts={toasts} />
      <div className="dashboard-header">
        <h1 className="dashboard-title">Delivery</h1>
        <span className="live-pill">● Live</span>
      </div>
      <p className="dashboard-sub">Signed in as <strong>{username}</strong></p>

      {loading && <LoadingState message="Loading delivery orders…" />}
      {error && (
        <ErrorState
          message="Couldn't load delivery orders. Is the API running?"
          onRetry={reload}
        />
      )}

      {!loading && !error && (
        <div className="kitchen-columns">
          <div className="kitchen-col">
            <h2 className="col-heading">Ready for Pickup <span className="count-badge">{ready.length}</span></h2>
            {ready.length === 0 && <p className="empty-state">Nothing ready yet — orders will appear here live.</p>}
            {ready.map(order => (
              <OrderCard key={order.id} order={order} actions={
                <button className="btn btn-start" onClick={() => handleClaim(order.id)}>Claim Order</button>
              } />
            ))}
          </div>

          <div className="kitchen-col">
            <h2 className="col-heading">Out for Delivery <span className="count-badge">{outForDelivery.length}</span></h2>
            {outForDelivery.length === 0 && <p className="empty-state">No active deliveries.</p>}
            {outForDelivery.map(order => (
              <OrderCard key={order.id} order={order} actions={
                order.claimedByDeliveryUser === username
                  ? <button className="btn btn-ready" onClick={() => handleDeliver(order.id)}>Mark Delivered</button>
                  : <span className="claimed-by">Claimed by {order.claimedByDeliveryUser}</span>
              } />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
