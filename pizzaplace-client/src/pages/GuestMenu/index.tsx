import { useState } from 'react';
import { getMenu } from '../../api/menu';
import { placeOrder } from '../../api/orders';
import { extractApiError } from '../../api/errors';
import { useAsync } from '../../hooks/useAsync';
import { useCart } from '../../hooks/useCart';
import type { Order } from '../../types';
import { useToast, ToastContainer } from '../../components/Toast';
import { StatusBadge } from '../../components/StatusBadge';
import { Tooltip } from '../../components/Tooltip';
import { LoadingState } from '../../components/LoadingState';
import { ErrorState } from '../../components/ErrorState';
import { useOrderHub } from '../../signalr/useOrderHub';

export function GuestMenu() {
  const { data: pizzas, loading: menuLoading, error: menuError, reload: reloadMenu } = useAsync(getMenu, []);
  const { items: cart, add: addToCart, remove: removeFromCart, reset: resetCart, total: totalPrice, count: totalItems } = useCart();
  const [customerName, setCustomerName] = useState('');
  const [placedOrder, setPlacedOrder] = useState<Order | null>(null);
  const [ordering, setOrdering] = useState(false);
  const { toasts, addToast } = useToast();

  useOrderHub('Guest', {
    OrderStatusChanged: (updated: unknown) => {
      const o = updated as Order;
      if (placedOrder && o.id === placedOrder.id) setPlacedOrder(o);
    }
  });

  const handleOrder = async () => {
    if (!customerName.trim()) { addToast('Please enter your name.', 'error'); return; }
    if (cart.length === 0) { addToast('Add something to your cart first.', 'error'); return; }
    setOrdering(true);
    try {
      const order = await placeOrder(customerName, cart.map(i => ({ pizzaId: i.pizza.id, quantity: i.quantity })));
      setPlacedOrder(order);
      resetCart();
      addToast(`Order placed! €${order.totalPrice.toFixed(2)} charged. Order #${order.id}`, 'success');
    } catch (err) {
      addToast(extractApiError(err, 'Something went wrong. Please try again.'), 'error');
    } finally {
      setOrdering(false);
    }
  };

  return (
    <div className="guest-page">
      <ToastContainer toasts={toasts} />
      <header className="guest-header">
        <div className="guest-header-inner">
          <div className="guest-brand">
            <span className="brand-icon">🍕</span>
            <span className="brand-name">PizzaPlace</span>
          </div>
          {totalItems > 0 && (
            <span className="cart-chip">{totalItems} in cart · €{totalPrice.toFixed(2)}</span>
          )}
        </div>
      </header>

      <section className="hero">
        <h1 className="hero-title">Fresh from the oven,<br />right to your door.</h1>
        <p className="hero-sub">Order in seconds. No account needed.</p>
      </section>

      <main className="guest-main">
        {menuLoading && <LoadingState message="Loading menu…" />}
        {menuError && (
          <ErrorState
            message="Couldn't load the menu. Is the server running?"
            onRetry={reloadMenu}
          />
        )}
        {!menuLoading && !menuError && pizzas && (
          <div className="menu-grid">
            {pizzas.map(pizza => (
              <div key={pizza.id} className="pizza-card">
                {pizza.imageUrl && (
                  <div className="pizza-img-wrap">
                    <img src={pizza.imageUrl} alt={pizza.name} className="pizza-img" loading="lazy" />
                  </div>
                )}
                <div className="pizza-info">
                  <div className="pizza-name">{pizza.name}</div>
                  <div className="pizza-desc">{pizza.description}</div>
                  <div className="pizza-footer">
                    <span className="pizza-price">€{pizza.price.toFixed(2)}</span>
                    <button className="btn btn-add" onClick={() => addToCart(pizza)}>+ Add</button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        {cart.length > 0 && (
          <div className="cart-panel">
            <h2 className="cart-title">Your Order</h2>
            <div className="cart-name-row">
              <input
                className="input"
                placeholder="Your name"
                value={customerName}
                onChange={e => setCustomerName(e.target.value)}
              />
            </div>
            <ul className="cart-list">
              {cart.map(item => (
                <li key={item.pizza.id} className="cart-item">
                  <span>{item.quantity}× {item.pizza.name}</span>
                  <span className="cart-item-right">
                    €{(item.pizza.price * item.quantity).toFixed(2)}
                    <button className="btn-remove" onClick={() => removeFromCart(item.pizza.id)}>✕</button>
                  </span>
                </li>
              ))}
            </ul>
            <div className="cart-total">Total: €{totalPrice.toFixed(2)}</div>
            <button className="btn btn-primary btn-full" onClick={handleOrder} disabled={ordering}>
              {ordering ? 'Placing order…' : 'Place Order — Pay Now'}
            </button>
          </div>
        )}

        {placedOrder && (
          <div className="tracker-panel">
            {(placedOrder.status === 'Delivered' || placedOrder.status === 'Cancelled') && (
              <Tooltip content="Dismiss" className="tracker-dismiss">
                <button
                  className="tracker-dismiss-btn"
                  aria-label="Dismiss order tracker"
                  onClick={() => setPlacedOrder(null)}
                >
                  ✕
                </button>
              </Tooltip>
            )}
            <h2 className="tracker-title">Order #{placedOrder.id} — Live Tracking</h2>
            <p className="tracker-name">Hi {placedOrder.customerName}!</p>
            <div className="tracker-status">
              <StatusBadge status={placedOrder.status} />
            </div>
            {placedOrder.status === 'Cancelled' ? (
              <div className="tracker-cancelled" role="alert">
                <p className="tracker-cancelled-title">Order cancelled</p>
                {placedOrder.cancellationReason && (
                  <p className="tracker-cancelled-reason">{placedOrder.cancellationReason}</p>
                )}
              </div>
            ) : (
              <div className="tracker-timeline">
                {(['Received', 'Preparing', 'Ready', 'OutForDelivery', 'Delivered'] as const).map((s, i, arr) => {
                  const idx = arr.indexOf(placedOrder.status);
                  const done = i <= idx;
                  return (
                    <div key={s} className={`timeline-step ${done ? 'done' : ''}`}>
                      <div className="timeline-dot" />
                      <span>{s === 'OutForDelivery' ? 'Out for Delivery' : s}</span>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}
