import { useState } from 'react';
import { AuthProvider, useAuth } from './auth/AuthContext';
import { LoginPage } from './auth/LoginPage';
import { GuestMenu } from './pages/GuestMenu';
import { ChefDashboard } from './pages/ChefDashboard';
import { DeliveryBoard } from './pages/DeliveryBoard';
import { SupervisorPanel } from './pages/SupervisorPanel';

function StaffNav() {
  const { auth, signOut } = useAuth();
  return (
    <nav className="staff-nav">
      <span className="staff-nav-brand">🍕 PizzaPlace</span>
      <span className="staff-nav-role">{auth?.role} — {auth?.username}</span>
      <button className="btn btn-ghost" onClick={signOut}>Sign out</button>
    </nav>
  );
}

function AppInner() {
  const { auth } = useAuth();
  const [showLogin, setShowLogin] = useState(false);

  if (auth) {
    return (
      <div className="staff-layout">
        <StaffNav />
        <div className="staff-content">
          {auth.role === 'Chef' && <ChefDashboard token={auth.token} />}
          {auth.role === 'Delivery' && <DeliveryBoard token={auth.token} username={auth.username} />}
          {auth.role === 'Supervisor' && <SupervisorPanel token={auth.token} />}
        </div>
      </div>
    );
  }

  if (showLogin) return <LoginPage />;

  return (
    <div>
      <GuestMenu />
      <button className="staff-entry-btn" onClick={() => setShowLogin(true)}>Staff Login</button>
    </div>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <AppInner />
    </AuthProvider>
  );
}
