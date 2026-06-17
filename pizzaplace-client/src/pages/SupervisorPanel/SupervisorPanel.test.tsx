import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { SupervisorPanel } from './index';

vi.mock('../../api/supervisor');
vi.mock('../../api/orders');
vi.mock('../../signalr/useOrderHub', () => ({ useOrderHub: vi.fn() }));

import { getInventory, getStats, restockIngredient } from '../../api/supervisor';
import { getAllOrders } from '../../api/orders';

const mockInventory = [
  { id: 1, name: 'Mozzarella',  stockQuantity: 10, unit: 'g',     lowStockThreshold: 800, isRestocking: false, isLow: true,  demandFromOrders: 0, hasShortage: false, deficit: 0, ordersWithDemand: 0 },
  { id: 2, name: 'Pizza Dough', stockQuantity: 20, unit: 'balls', lowStockThreshold: 5,   isRestocking: false, isLow: false, demandFromOrders: 0, hasShortage: false, deficit: 0, ordersWithDemand: 0 },
];

const emptyStats = {
  ordersToday: 0, revenueToday: 0, revenueTotal: 0,
  totalOrders: 0, delivered: 0, mostPopularPizza: null,
};

describe('SupervisorPanel — inventory tab', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getInventory).mockResolvedValue(mockInventory);
    vi.mocked(getAllOrders).mockResolvedValue([]);
    vi.mocked(getStats).mockResolvedValue(emptyStats);
  });

  it('shows ⚠ Low stock only for ingredients below threshold', async () => {
    render(<SupervisorPanel token="mock-token" />);

    // Inventory tab is default; wait for data
    await screen.findByText('Mozzarella');
    await screen.findByText('Pizza Dough');

    // Only Mozzarella is low — exactly one warning in the whole panel
    expect(screen.getAllByText('⚠ Low stock')).toHaveLength(1);

    // Mozzarella card has the warning
    expect(screen.getByText('⚠ Low stock')).toBeInTheDocument();
  });

  it('does NOT show ⚠ Low stock for healthy ingredients', async () => {
    render(<SupervisorPanel token="mock-token" />);

    await screen.findByText('Pizza Dough');

    // Get all inventory cards — Pizza Dough card should have no low-stock sibling
    const allWarnings = screen.queryAllByText('⚠ Low stock');
    expect(allWarnings).toHaveLength(1); // only Mozzarella
  });

  it('shows a shortage badge with the deficit only for ingredients with hasShortage', async () => {
    vi.mocked(getInventory).mockResolvedValue([
      { id: 1, name: 'Tomato Sauce', stockQuantity: 130, unit: 'ml', lowStockThreshold: 500, isRestocking: false, isLow: false, demandFromOrders: 160, hasShortage: true,  deficit: 30, ordersWithDemand: 2 },
      { id: 2, name: 'Pizza Dough',  stockQuantity: 20,  unit: 'balls', lowStockThreshold: 5, isRestocking: false, isLow: false, demandFromOrders: 2,   hasShortage: false, deficit: 0,  ordersWithDemand: 1 },
    ]);

    render(<SupervisorPanel token="mock-token" />);

    await screen.findByText('Tomato Sauce');
    await screen.findByText('Pizza Dough');

    // Exactly one shortage marker, on the short ingredient, showing the deficit
    expect(screen.getByText('⚠ -30ml')).toBeInTheDocument();
    expect(screen.getAllByText(/⚠ -\d/)).toHaveLength(1);
  });

  it('reveals the shortage breakdown in a tooltip on hover and hides it on leave', async () => {
    vi.mocked(getInventory).mockResolvedValue([
      { id: 1, name: 'Tomato Sauce', stockQuantity: 130, unit: 'ml', lowStockThreshold: 500, isRestocking: false, isLow: false, demandFromOrders: 160, hasShortage: true, deficit: 30, ordersWithDemand: 2 },
    ]);

    render(<SupervisorPanel token="mock-token" />);

    const badge = await screen.findByText('⚠ -30ml');
    const trigger = badge.closest('.tooltip-trigger')!;

    fireEvent.mouseEnter(trigger);
    // Custom tooltip (not the native title attr) appears after the hover delay
    const tip = await screen.findByRole('tooltip');
    expect(tip).toHaveTextContent('Needed 160ml across 2 orders · have 130ml · short 30ml');

    fireEvent.mouseLeave(trigger);
    await waitFor(() => expect(screen.queryByRole('tooltip')).not.toBeInTheDocument());
  });

  it('shows a ~5 second restock toast when Restock is clicked', async () => {
    vi.mocked(restockIngredient).mockResolvedValue(undefined);
    render(<SupervisorPanel token="mock-token" />);

    await screen.findByText('Mozzarella');
    fireEvent.click(screen.getAllByText('Restock')[0]);

    expect(await screen.findByText(/completes in ~5 seconds/)).toBeInTheDocument();
  });
});
