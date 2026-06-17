import { api } from './client';
import type { Order } from '../types';

export const getDeliveryOrders = () =>
  api.get<Order[]>('/api/delivery/orders').then(r => r.data);

export const claimOrder = (id: number) =>
  api.post<Order>(`/api/delivery/orders/${id}/claim`).then(r => r.data);

export const deliverOrder = (id: number) =>
  api.post<Order>(`/api/delivery/orders/${id}/deliver`).then(r => r.data);
