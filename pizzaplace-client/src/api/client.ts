import axios from 'axios';

export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5221';

export const api = axios.create({ baseURL: API_BASE });

export function setAuthToken(token: string | null) {
  if (token) api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  else delete api.defaults.headers.common['Authorization'];
}
