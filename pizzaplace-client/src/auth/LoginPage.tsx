import { useState, type FormEvent } from 'react';
import { login } from '../api/auth';
import { useAuth } from './AuthContext';
import type { AuthState } from '../types';

export function LoginPage() {
  const { signIn } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const data = await login(username, password);
      signIn(data as AuthState);
    } catch {
      setError('Invalid credentials. Try chef/chef, delivery/delivery, or supervisor/supervisor.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">🍕</div>
        <h1 className="login-title">Staff Login</h1>
        <p className="login-subtitle">PizzaPlace Operations</p>
        <form onSubmit={handleSubmit} className="login-form">
          <input
            className="input"
            type="text"
            placeholder="Username"
            value={username}
            onChange={e => setUsername(e.target.value)}
            required
            autoFocus
          />
          <input
            className="input"
            type="password"
            placeholder="Password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            required
          />
          {error && <p className="login-error">{error}</p>}
          <button className="btn btn-primary" type="submit" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign In'}
          </button>
        </form>
        <p className="login-hint">Demo credentials: chef / delivery / supervisor (same as password)</p>
      </div>
    </div>
  );
}
