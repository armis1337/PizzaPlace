import { createContext, useContext, useState, type ReactNode } from 'react';
import type { AuthState } from '../types';
import { setAuthToken } from '../api/client';

interface AuthContextValue {
  auth: AuthState | null;
  signIn: (state: AuthState) => void;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<AuthState | null>(() => {
    const stored = sessionStorage.getItem('pizzaplace_auth');
    if (stored) {
      const parsed = JSON.parse(stored) as AuthState;
      setAuthToken(parsed.token);
      return parsed;
    }
    return null;
  });

  const signIn = (state: AuthState) => {
    setAuth(state);
    setAuthToken(state.token);
    sessionStorage.setItem('pizzaplace_auth', JSON.stringify(state));
  };

  const signOut = () => {
    setAuth(null);
    setAuthToken(null);
    sessionStorage.removeItem('pizzaplace_auth');
  };

  return <AuthContext.Provider value={{ auth, signIn, signOut }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
