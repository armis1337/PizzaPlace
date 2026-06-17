import { render, screen, waitFor, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { GuestMenu } from './index';

vi.mock('../../api/menu');
vi.mock('../../api/orders');
vi.mock('../../signalr/useOrderHub', () => ({ useOrderHub: vi.fn() }));

import { getMenu } from '../../api/menu';
import { placeOrder } from '../../api/orders';
import { useOrderHub } from '../../signalr/useOrderHub';

const mockPizzas = [
  { id: 1, name: 'Margherita', description: 'Classic', price: 12.5,  imageUrl: null },
  { id: 2, name: 'Pepperoni',  description: 'Spicy',   price: 14.5,  imageUrl: null },
];

const mockOrder = {
  id: 42,
  customerName: 'Alice',
  status: 'Received' as const,
  totalPrice: 12.5,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  claimedByDeliveryUser: null,
  cancellationReason: null,
  items: [{ pizzaId: 1, pizzaName: 'Margherita', quantity: 1 }],
};

describe('GuestMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getMenu).mockResolvedValue(mockPizzas);
    vi.mocked(placeOrder).mockResolvedValue(mockOrder);
  });

  it('calls placeOrder with customer name and correct items payload', async () => {
    const user = userEvent.setup();
    render(<GuestMenu />);

    // Wait for async menu load
    const addButtons = await screen.findAllByText('+ Add');

    await user.click(addButtons[0]); // add Margherita

    await user.type(screen.getByPlaceholderText('Your name'), 'Alice');
    await user.click(screen.getByText('Place Order — Pay Now'));

    expect(placeOrder).toHaveBeenCalledWith('Alice', [{ pizzaId: 1, quantity: 1 }]);
  });

  it('increments quantity rather than adding a duplicate line when the same pizza is added twice', async () => {
    const user = userEvent.setup();
    render(<GuestMenu />);

    const addButtons = await screen.findAllByText('+ Add');

    await user.click(addButtons[0]); // first click → quantity 1
    await user.click(addButtons[0]); // second click → quantity 2, NOT two lines

    // Cart chip shows count + total: 12.5 × 2 = 25.00
    expect(screen.getByText(/2 in cart · €25\.00/)).toBeInTheDocument();

    // Only one cart line item for Margherita (quantity 2, not two separate lines)
    expect(screen.getAllByText(/Margherita/)).toHaveLength(2); // one in menu, one in cart
    expect(screen.getByText('2× Margherita')).toBeInTheDocument();
  });

  it('clears the cart after a successful order', async () => {
    const user = userEvent.setup();
    render(<GuestMenu />);

    await user.click((await screen.findAllByText('+ Add'))[0]);
    expect(screen.getByText('1× Margherita')).toBeInTheDocument();

    await user.type(screen.getByPlaceholderText('Your name'), 'Alice');
    await user.click(screen.getByText('Place Order — Pay Now'));

    // Cart panel must disappear on success — no partial re-order possible
    await waitFor(() => {
      expect(screen.queryByText('1× Margherita')).not.toBeInTheDocument();
      expect(screen.queryByText('Place Order — Pay Now')).not.toBeInTheDocument();
    });
  });

  it('keeps the cart when placeOrder fails', async () => {
    vi.mocked(placeOrder).mockRejectedValueOnce(new Error('Server error'));
    const user = userEvent.setup();
    render(<GuestMenu />);

    await user.click((await screen.findAllByText('+ Add'))[0]);
    await user.type(screen.getByPlaceholderText('Your name'), 'Alice');
    await user.click(screen.getByText('Place Order — Pay Now'));

    // Cart must survive a failure so the guest doesn't lose their selection
    await waitFor(() => {
      expect(screen.getByText('1× Margherita')).toBeInTheDocument();
      expect(screen.getByText('Place Order — Pay Now')).toBeInTheDocument();
    });
  });

  it('shows a cancelled message with the reason on the tracking view when the order is cancelled', async () => {
    // Capture the SignalR handlers GuestMenu registers so we can simulate a chef cancellation
    type Handlers = { OrderStatusChanged: (o: unknown) => void };
    let handlers: Handlers | undefined;
    vi.mocked(useOrderHub).mockImplementation((_group, h) => { handlers = h as Handlers; });

    const user = userEvent.setup();
    render(<GuestMenu />);

    await user.click((await screen.findAllByText('+ Add'))[0]);
    await user.type(screen.getByPlaceholderText('Your name'), 'Alice');
    await user.click(screen.getByText('Place Order — Pay Now'));

    await screen.findByText(/Live Tracking/); // tracker visible after placement

    // Chef cancels → server pushes the cancelled order over SignalR
    act(() => handlers!.OrderStatusChanged({
      ...mockOrder, status: 'Cancelled', cancellationReason: 'Out of basil',
    }));

    expect(await screen.findByText('Order cancelled')).toBeInTheDocument();
    expect(screen.getByText('Out of basil')).toBeInTheDocument();
    // The normal progress timeline should not be shown for a cancelled order
    expect(screen.queryByText('Out for Delivery')).not.toBeInTheDocument();
  });

  it('shows a Dismiss control only for terminal-state orders and hides the tracker when clicked', async () => {
    type Handlers = { OrderStatusChanged: (o: unknown) => void };
    let handlers: Handlers | undefined;
    vi.mocked(useOrderHub).mockImplementation((_group, h) => { handlers = h as Handlers; });

    const user = userEvent.setup();
    render(<GuestMenu />);

    await user.click((await screen.findAllByText('+ Add'))[0]);
    await user.type(screen.getByPlaceholderText('Your name'), 'Alice');
    await user.click(screen.getByText('Place Order — Pay Now'));

    await screen.findByText(/Live Tracking/); // tracker visible after placement

    // In progress (Received) → no dismiss control
    expect(screen.queryByRole('button', { name: 'Dismiss order tracker' })).not.toBeInTheDocument();

    // Terminal state (Delivered) → dismiss control appears
    act(() => handlers!.OrderStatusChanged({ ...mockOrder, status: 'Delivered' }));
    const dismiss = await screen.findByRole('button', { name: 'Dismiss order tracker' });

    await user.click(dismiss);

    // Tracker section is removed, leaving the clean menu
    expect(screen.queryByText(/Live Tracking/)).not.toBeInTheDocument();
  });

  it('removes an item from the cart when ✕ is clicked', async () => {
    const user = userEvent.setup();
    render(<GuestMenu />);

    const addButtons = await screen.findAllByText('+ Add');
    await user.click(addButtons[0]); // add Margherita

    expect(screen.getByText('1× Margherita')).toBeInTheDocument();

    await user.click(screen.getByText('✕'));

    // Cart panel should disappear once empty
    expect(screen.queryByText('1× Margherita')).not.toBeInTheDocument();
    expect(screen.queryByText('Place Order — Pay Now')).not.toBeInTheDocument();
  });
});
