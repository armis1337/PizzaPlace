import { useEffect, useState } from 'react';
import { getInventory, restockIngredient, getStats } from '../../api/supervisor';
import { getAllOrders } from '../../api/orders';
import { extractApiError } from '../../api/errors';
import { useAsync } from '../../hooks/useAsync';
import type { Ingredient, Order } from '../../types';
import { OrderCard } from '../../components/OrderCard';
import { Tooltip } from '../../components/Tooltip';
import { StatusBadge } from '../../components/StatusBadge';
import { LoadingState } from '../../components/LoadingState';
import { ErrorState } from '../../components/ErrorState';
import { useToast, ToastContainer } from '../../components/Toast';
import { useOrderHub } from '../../signalr/useOrderHub';

type Tab = 'inventory' | 'orders' | 'stats';

export function SupervisorPanel({ token }: { token: string }) {
  const [tab, setTab] = useState<Tab>('inventory');
  const { toasts, addToast } = useToast();

  // Each data source has its own loading/error/reload state
  const { data: fetchedInventory, loading: invLoading, error: invError, reload: reloadInventory } = useAsync(getInventory, []);
  const { data: fetchedOrders,    loading: ordLoading, error: ordError, reload: reloadOrders    } = useAsync(getAllOrders, []);
  const { data: stats,            loading: stLoading,  error: stError,  reload: reloadStats     } = useAsync(getStats, []);

  // Local inventory state — supports optimistic "Restocking…" update before SignalR fires
  const [inventory, setInventory] = useState<Ingredient[]>([]);
  useEffect(() => { if (fetchedInventory !== undefined) setInventory(fetchedInventory); }, [fetchedInventory]);

  // Local orders state — updated in-place by SignalR without a full reload
  const [orders, setOrders] = useState<Order[]>([]);
  useEffect(() => { if (fetchedOrders !== undefined) setOrders(fetchedOrders); }, [fetchedOrders]);

  useOrderHub('Supervisor', {
    OrderStatusChanged: (order: unknown) => {
      const o = order as Order;
      setOrders(prev => {
        const exists = prev.find(x => x.id === o.id);
        return exists ? prev.map(x => x.id === o.id ? o : x) : [o, ...prev];
      });
      reloadStats();
      // A new/changed order shifts demand even when stock didn't move — refresh shortages.
      reloadInventory();
      addToast(`Order #${o.id} → ${o.status}`, 'info');
    },
    InventoryChanged: () => reloadInventory()
  }, token);

  const handleRestock = async (id: number) => {
    try {
      await restockIngredient(id);
      setInventory(prev => prev.map(i => i.id === id ? { ...i, isRestocking: true } : i));
      addToast('Restock started — completes in ~5 seconds', 'success');
    } catch (err) {
      addToast(extractApiError(err, 'Failed to restock'), 'error');
    }
  };

  return (
    <div className="dashboard">
      <ToastContainer toasts={toasts} />
      <div className="dashboard-header">
        <h1 className="dashboard-title">Supervisor</h1>
        <span className="live-pill">● Live</span>
      </div>

      <div className="tabs">
        {(['inventory', 'orders', 'stats'] as Tab[]).map(t => (
          <button key={t} className={`tab ${tab === t ? 'tab-active' : ''}`} onClick={() => setTab(t)}>
            {t.charAt(0).toUpperCase() + t.slice(1)}
          </button>
        ))}
      </div>

      {tab === 'inventory' && (
        <>
          {invLoading && <LoadingState message="Loading inventory…" />}
          {invError && <ErrorState message="Couldn't load inventory. Is the API running?" onRetry={reloadInventory} />}
          {!invLoading && !invError && (
            <div className="inventory-grid">
              {inventory.map(ing => (
                <div key={ing.id} className={`inventory-card ${ing.hasShortage ? 'is-short' : ing.isLow ? 'is-low' : ''}`}>
                  <div className="inv-name">{ing.name}</div>
                  <div className="inv-stock">
                    <span className={`inv-qty ${ing.isLow ? 'qty-low' : ''}`}>{ing.stockQuantity}</span>
                    <span className="inv-unit">{ing.unit}</span>
                  </div>
                  <div className="inv-badges">
                    {ing.isLow && <span className="inv-badge inv-badge-low">⚠ Low stock</span>}
                    {ing.hasShortage && (
                      <Tooltip
                        content={`Needed ${ing.demandFromOrders}${ing.unit} across ${ing.ordersWithDemand} order${ing.ordersWithDemand === 1 ? '' : 's'} · have ${ing.stockQuantity}${ing.unit} · short ${ing.deficit}${ing.unit}`}
                      >
                        <span className="inv-badge inv-badge-short">⚠ -{ing.deficit}{ing.unit}</span>
                      </Tooltip>
                    )}
                    {ing.isRestocking && <span className="inv-badge inv-badge-restock">⟳ Restocking</span>}
                  </div>
                  <button
                    className="btn btn-restock"
                    disabled={ing.isRestocking}
                    onClick={() => handleRestock(ing.id)}
                  >
                    {ing.isRestocking ? 'Restocking…' : 'Restock'}
                  </button>
                </div>
              ))}
            </div>
          )}
        </>
      )}

      {tab === 'orders' && (
        <>
          {ordLoading && <LoadingState message="Loading orders…" />}
          {ordError && <ErrorState message="Couldn't load orders. Is the API running?" onRetry={reloadOrders} />}
          {!ordLoading && !ordError && (
            <div>
              <div className="orders-status-strip">
                {(['Received', 'Preparing', 'Ready', 'OutForDelivery', 'Delivered', 'Cancelled'] as const).map(s => (
                  <div key={s} className="status-count">
                    <StatusBadge status={s} />
                    <span className="status-num">{orders.filter(o => o.status === s).length}</span>
                  </div>
                ))}
              </div>
              <div className="orders-list">
                {orders.length === 0 && <p className="empty-state">No orders yet.</p>}
                {orders.map(order => <OrderCard key={order.id} order={order} />)}
              </div>
            </div>
          )}
        </>
      )}

      {tab === 'stats' && (
        <>
          {stLoading && <LoadingState message="Loading stats…" />}
          {stError && <ErrorState message="Couldn't load stats. Is the API running?" onRetry={reloadStats} />}
          {!stLoading && !stError && stats && (
            <div className="stats-grid">
              <div className="stat-card">
                <div className="stat-value">{stats.ordersToday}</div>
                <div className="stat-label">Orders Today</div>
              </div>
              <div className="stat-card">
                <div className="stat-value">€{stats.revenueToday.toFixed(2)}</div>
                <div className="stat-label">Revenue Today</div>
              </div>
              <div className="stat-card">
                <div className="stat-value">€{stats.revenueTotal.toFixed(2)}</div>
                <div className="stat-label">Total Revenue</div>
              </div>
              <div className="stat-card">
                <div className="stat-value">{stats.totalOrders}</div>
                <div className="stat-label">All-Time Orders</div>
              </div>
              <div className="stat-card">
                <div className="stat-value">{stats.delivered}</div>
                <div className="stat-label">Delivered</div>
              </div>
              {stats.mostPopularPizza && (
                <div className="stat-card stat-featured">
                  <div className="stat-value">🍕 {stats.mostPopularPizza.pizza}</div>
                  <div className="stat-label">Most Popular · {stats.mostPopularPizza.count} sold</div>
                </div>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}
