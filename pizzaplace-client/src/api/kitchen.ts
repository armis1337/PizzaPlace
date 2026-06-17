import { api } from './client';
import type { Order } from '../types';

export const getKitchenOrders = () =>
  api.get<Order[]>('/api/kitchen/orders').then(r => r.data);

export const startOrder = (id: number) =>
  api.post<Order>(`/api/kitchen/orders/${id}/start`).then(r => r.data);

export const markReady = (id: number) =>
  api.post<Order>(`/api/kitchen/orders/${id}/ready`).then(r => r.data);

export const cancelOrder = (id: number, reason?: string) =>
  api.post<Order>(`/api/kitchen/orders/${id}/cancel`, { reason: reason ?? null }).then(r => r.data);
