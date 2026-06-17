import { useEffect, useState } from 'react';
import { getKitchenOrders, startOrder, markReady, cancelOrder } from '../../api/kitchen';
import { extractApiError } from '../../api/errors';
import { useAsync } from '../../hooks/useAsync';
import type { Order } from '../../types';
import { OrderCard } from '../../components/OrderCard';
import { Modal } from '../../components/Modal';
import { LoadingState } from '../../components/LoadingState';
import { ErrorState } from '../../components/ErrorState';
import { useToast, ToastContainer } from '../../components/Toast';
import { useOrderHub } from '../../signalr/useOrderHub';

export function ChefDashboard({ token }: { token: string }) {
  const { data: initialOrders, loading, error, reload } = useAsync(getKitchenOrders, []);
  const [orders, setOrders] = useState<Order[]>([]);
  const [cancellingId, setCancellingId] = useState<number | null>(null);
  const [cancelReason, setCancelReason] = useState('');
  const { toasts, addToast } = useToast();

  // Sync fetched data into local state (local state also receives SignalR updates)
  useEffect(() => {
    if (initialOrders !== undefined) setOrders(initialOrders);
  }, [initialOrders]);

  useOrderHub('Chef', {
    OrderReceived: (order: unknown) => {
      const o = order as Order;
      addToast(`New order #${o.id} from ${o.customerName}!`, 'info');
      // Re-fetch so the new order and every other order get fresh canStart values.
      reload();
    },
    // Stock changed (a start elsewhere, or a restock) — recompute canStart for all orders.
    InventoryChanged: () => reload()
  }, token);

  const handleStart = async (id: number) => {
    try {
      const updated = await startOrder(id);
      setOrders(prev => prev.map(o => o.id === id ? updated : o));
      addToast(`Order #${id} moved to Preparing`, 'success');
      // Starting consumed stock — re-fetch so other orders' canStart re-evaluate.
      reload();
    } catch (err) {
      addToast(extractApiError(err, 'Failed to start order'), 'error');
    }
  };

  const handleReady = async (id: number) => {
    try {
      const updated = await markReady(id);
      setOrders(prev => prev.filter(o => o.id !== updated.id));
      addToast(`Order #${id} is Ready — delivery notified`, 'success');
    } catch (err) {
      addToast(extractApiError(err, 'Failed to mark ready'), 'error');
    }
  };

  const closeCancelModal = () => { setCancellingId(null); setCancelReason(''); };

  const handleCancel = async (id: number, reason: string) => {
    try {
      const cancelled = await cancelOrder(id, reason.trim() || undefined);
      setOrders(prev => prev.filter(o => o.id !== cancelled.id));
      addToast(`Order #${id} cancelled`, 'success');
    } catch (err) {
      addToast(extractApiError(err, 'Failed to cancel order'), 'error');
    } finally {
      closeCancelModal();
    }
  };

  const received = orders.filter(o => o.status === 'Received');
  const preparing = orders.filter(o => o.status === 'Preparing');
  const cancellingOrder = orders.find(o => o.id === cancellingId) ?? null;

  return (
    <div className="dashboard">
      <ToastContainer toasts={toasts} />
      <div className="dashboard-header">
        <h1 className="dashboard-title">Kitchen</h1>
        <span className="live-pill">● Live</span>
      </div>

      {loading && <LoadingState message="Loading kitchen queue…" />}
      {error && (
        <ErrorState
          message="Couldn't load the kitchen queue. Is the API running?"
          onRetry={reload}
        />
      )}

      {!loading && !error && (
        <div className="kitchen-columns">
          <div className="kitchen-col">
            <h2 className="col-heading">Queue <span className="count-badge">{received.length}</span></h2>
            {received.length === 0 && <p className="empty-state">No new orders — take a breath.</p>}
            {received.map(order => {
              const blocked = order.canStart === false;
              return (
                <OrderCard
                  key={order.id}
                  order={order}
                  actions={
                    <>
                      <button
                        className="btn btn-start"
                        onClick={() => handleStart(order.id)}
                        disabled={blocked}
                      >
                        Start Preparing
                      </button>
                      <button className="btn btn-cancel" onClick={() => { setCancellingId(order.id); setCancelReason(''); }}>Cancel</button>
                    </>
                  }
                  note={blocked
                    ? <>⚠ Can't start — short on {order.blockingIngredients?.join(', ')}</>
                    : undefined}
                />
              );
            })}
          </div>

          <div className="kitchen-col">
            <h2 className="col-heading">Preparing <span className="count-badge">{preparing.length}</span></h2>
            {preparing.length === 0 && <p className="empty-state">Nothing in progress.</p>}
            {preparing.map(order => (
              <OrderCard key={order.id} order={order} actions={
                <button className="btn btn-ready" onClick={() => handleReady(order.id)}>Mark Ready</button>
              } />
            ))}
          </div>
        </div>
      )}

      {cancellingOrder && (
        <Modal
          title={`Cancel order #${cancellingOrder.id}?`}
          onClose={closeCancelModal}
          footer={
            <>
              <button className="btn btn-ghost" onClick={closeCancelModal}>Keep order</button>
              <button className="btn btn-cancel" onClick={() => handleCancel(cancellingOrder.id, cancelReason)}>
                Confirm Cancel
              </button>
            </>
          }
        >
          <div className="modal-summary">
            <div className="modal-summary-name">{cancellingOrder.customerName}</div>
            <ul className="modal-summary-items">
              {cancellingOrder.items.map((item, i) => (
                <li key={i}>{item.quantity}× {item.pizzaName}</li>
              ))}
            </ul>
          </div>
          <label className="modal-label" htmlFor="cancel-reason">Reason (optional)</label>
          <textarea
            id="cancel-reason"
            className="modal-textarea"
            placeholder="Reason (optional)"
            value={cancelReason}
            autoFocus
            onChange={e => setCancelReason(e.target.value)}
          />
        </Modal>
      )}
    </div>
  );
}
