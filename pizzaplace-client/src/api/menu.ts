import { api } from './client';
import type { Pizza } from '../types';

export const getMenu = () => api.get<Pizza[]>('/api/menu').then(r => r.data);
