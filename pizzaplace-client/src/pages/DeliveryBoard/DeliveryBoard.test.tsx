import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { DeliveryBoard } from './index';

vi.mock('../../api/delivery');
vi.mock('../../signalr/useOrderHub', () => ({ useOrderHub: vi.fn() }));

import { getDeliveryOrders, claimOrder } from '../../api/delivery';

const mockReadyOrder = {
  id: 5,
  customerName: 'Charlie',
  status: 'Ready' as const,
  totalPrice: 14.5,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  claimedByDeliveryUser: null,
  items: [{ pizzaId: 2, pizzaName: 'Pepperoni', quantity: 1 }],
};

describe('DeliveryBoard', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getDeliveryOrders).mockResolvedValue([mockReadyOrder]);
    vi.mocked(claimOrder).mockResolvedValue({
      ...mockReadyOrder,
      status: 'OutForDelivery',
      claimedByDeliveryUser: 'delivery',
    });
  });

  it('calls claimOrder with the order id when "Claim Order" is clicked', async () => {
    const user = userEvent.setup();
    render(<DeliveryBoard token="mock-token" username="delivery" />);

    await screen.findByText('Charlie');

    await user.click(screen.getByText('Claim Order'));

    expect(claimOrder).toHaveBeenCalledWith(5);
  });
});
