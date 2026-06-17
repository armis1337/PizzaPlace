import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { ChefDashboard } from './index';

vi.mock('../../api/kitchen');
vi.mock('../../signalr/useOrderHub', () => ({ useOrderHub: vi.fn() }));

import { getKitchenOrders, startOrder, cancelOrder } from '../../api/kitchen';

const mockOrder = {
  id: 7,
  customerName: 'Bob',
  status: 'Received' as const,
  totalPrice: 12.5,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  claimedByDeliveryUser: null,
  cancellationReason: null,
  items: [{ pizzaId: 1, pizzaName: 'Margherita', quantity: 1 }],
};

describe('ChefDashboard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getKitchenOrders).mockResolvedValue([mockOrder]);
    vi.mocked(startOrder).mockResolvedValue({ ...mockOrder, status: 'Preparing' });
    vi.mocked(cancelOrder).mockResolvedValue({ ...mockOrder, status: 'Cancelled' });
  });

  it('calls startOrder with the order id when "Start Preparing" is clicked', async () => {
    const user = userEvent.setup();
    render(<ChefDashboard token="mock-token" />);

    await screen.findByText('Bob'); // wait for async load

    await user.click(screen.getByText('Start Preparing'));

    expect(startOrder).toHaveBeenCalledWith(7);
  });

  it('cancels a Received order with the entered reason and removes it from the queue', async () => {
    const user = userEvent.setup();
    render(<ChefDashboard token="mock-token" />);

    await screen.findByText('Bob'); // wait for async load

    await user.click(screen.getByText('Cancel')); // open the reason form
    await user.type(screen.getByPlaceholderText('Reason (optional)'), 'Out of basil');
    await user.click(screen.getByText('Confirm Cancel'));

    expect(cancelOrder).toHaveBeenCalledWith(7, 'Out of basil');

    // Cancelled order drops off the active queue
    await waitFor(() => expect(screen.queryByText('Bob')).not.toBeInTheDocument());
  });

  it('cancels with no reason when the field is left empty', async () => {
    const user = userEvent.setup();
    render(<ChefDashboard token="mock-token" />);

    await screen.findByText('Bob'); // wait for async load

    await user.click(screen.getByText('Cancel')); // open the modal
    await user.click(screen.getByText('Confirm Cancel')); // submit empty

    expect(cancelOrder).toHaveBeenCalledWith(7, undefined);
  });

  it('closes the modal without cancelling when "Keep order" is clicked', async () => {
    const user = userEvent.setup();
    render(<ChefDashboard token="mock-token" />);

    await screen.findByText('Bob'); // wait for async load

    await user.click(screen.getByText('Cancel')); // open the modal
    expect(screen.getByText('Cancel order #7?')).toBeInTheDocument();

    await user.click(screen.getByText('Keep order'));

    // Modal gone, no API call, order still in the queue
    expect(screen.queryByText('Cancel order #7?')).not.toBeInTheDocument();
    expect(cancelOrder).not.toHaveBeenCalled();
    expect(screen.getByText('Bob')).toBeInTheDocument();
  });

  it('disables Start (but keeps Cancel) when the order cannot be started', async () => {
    vi.mocked(getKitchenOrders).mockResolvedValue([
      { ...mockOrder, canStart: false, blockingIngredients: ['Tomato Sauce'] },
    ]);
    render(<ChefDashboard token="mock-token" />);

    await screen.findByText('Bob'); // wait for async load

    expect(screen.getByText('Start Preparing')).toBeDisabled();
    expect(screen.getByText('Cancel')).toBeEnabled();
    expect(screen.getByText(/Can't start — short on Tomato Sauce/)).toBeInTheDocument();
  });

  it('enables Start when the order can be started', async () => {
    vi.mocked(getKitchenOrders).mockResolvedValue([
      { ...mockOrder, canStart: true, blockingIngredients: [] },
    ]);
    render(<ChefDashboard token="mock-token" />);

    await screen.findByText('Bob'); // wait for async load

    expect(screen.getByText('Start Preparing')).toBeEnabled();
  });

  it('does NOT show a Cancel button on a Preparing order', async () => {
    vi.mocked(getKitchenOrders).mockResolvedValue([{ ...mockOrder, status: 'Preparing' }]);
    render(<ChefDashboard token="mock-token" />);

    await screen.findByText('Bob'); // wait for async load

    expect(screen.getByText('Mark Ready')).toBeInTheDocument();
    expect(screen.queryByText('Cancel')).not.toBeInTheDocument();
  });
});
