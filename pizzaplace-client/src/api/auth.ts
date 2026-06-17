import { api } from './client';
import type { AuthState } from '../types';

export const login = (username: string, password: string) =>
  api.post<AuthState>('/api/auth/login', { username, password }).then(r => r.data);
