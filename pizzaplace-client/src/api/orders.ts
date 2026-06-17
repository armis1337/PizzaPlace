import { api } from './client';
import type { Order, OrderStatus } from '../types';

export const placeOrder = (customerName: string, items: { pizzaId: number; quantity: number }[]) =>
  api.post<Order>('/api/orders', { customerName, items }).then(r => r.data);

export const getOrder = (id: number) =>
  api.get<Order>(`/api/orders/${id}`).then(r => r.data);

export const getAllOrders = (status?: OrderStatus) =>
  api.get<Order[]>('/api/orders', { params: status ? { status } : {} }).then(r => r.data);
