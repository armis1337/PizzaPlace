import { api } from './client';
import type { Ingredient, Stats } from '../types';

export const getInventory = () =>
  api.get<Ingredient[]>('/api/inventory').then(r => r.data);

export const restockIngredient = (id: number) =>
  api.post(`/api/inventory/${id}/restock`).then(r => r.data);

export const getStats = () =>
  api.get<Stats>('/api/stats/summary').then(r => r.data);
