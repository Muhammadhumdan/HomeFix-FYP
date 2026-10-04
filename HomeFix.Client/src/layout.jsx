import { useState } from 'react';
import { Link, NavLink, Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useAuth, homeFor } from './auth.jsx';

export function Navbar() {
  const { user, logout } = useAuth();
  const nav = useNavigate();
  const [open, setOpen] = useState(false);
  const go = () => setOpen(false);
  return (
    <header className="navbar">
      <div className="container nav-inner">
        <Link to="/" className="brand" onClick={go}>🏠 HOME<span>FIX</span></Link>
        <button className="nav-toggle" onClick={() => setOpen(!open)} aria-label="Menu">☰</button>
        <nav className={`nav-links ${open ? 'open' : ''}`}>
          <NavLink to="/" end onClick={go}>Home</NavLink>
          <NavLink to="/services" onClick={go}>Services</NavLink>
          <NavLink to="/search" onClick={go}>Search</NavLink>
          <NavLink to="/ai-search" onClick={go}>✨ AI Search</NavLink>
          <NavLink to="/about" onClick={go}>About</NavLink>
          {user ? (
            <>
              <Link className="btn btn-outline btn-sm" to={homeFor(user.role)} onClick={go}>Dashboard</Link>
              <button className="btn btn-primary btn-sm" onClick={() => { logout(); go(); nav('/'); }}>Logout</button>
            </>
          ) : (
            <>
              <Link className="btn btn-outline btn-sm" to="/login" onClick={go}>Login</Link>
              <Link className="btn btn-primary btn-sm" to="/register" onClick={go}>Register</Link>
            </>
          )}
        </nav>
      </div>
    </header>
  );
}

export function Footer() {
  return (
    <footer className="footer">
      <div className="container footer-inner">
        <div><strong>🏠 HOMEFIX</strong><p className="small">An AI-enhanced local services marketplace for Pakistan.</p></div>
        <div className="small">BSSE Final Year Project • Kohat University of Science &amp; Technology</div>
      </div>
    </footer>
  );
}

export function PublicLayout() {
  return (
    <>
      <Navbar />
      <main className="main"><Outlet /></main>
      <Footer />
    </>
  );
}

const MENUS = {
  Customer: [
    ['/customer', '📊', 'Dashboard', true], ['/customer/search', '🔎', 'Search'], ['/customer/ai-search', '✨', 'AI Search'],
    ['/customer/bookings', '📅', 'My Bookings'], ['/customer/profile', '👤', 'Profile']
  ],
  Provider: [
    ['/provider', '📊', 'Dashboard', true], ['/provider/requests', '📥', 'Booking Requests'], ['/provider/jobs', '🧰', 'Jobs'],
    ['/provider/services', '🛠️', 'My Services'], ['/provider/availability', '🗓️', 'Availability'],
    ['/provider/reviews', '⭐', 'Reviews'], ['/provider/profile', '👤', 'Profile']
  ],
  Admin: [
    ['/admin', '📊', 'Dashboard', true], ['/admin/approvals', '✅', 'Provider Approvals'], ['/admin/categories', '🗂️', 'Categories'],
    ['/admin/users', '👥', 'Users'], ['/admin/bookings', '📅', 'Bookings'], ['/admin/activity', '📈', 'Reports & Activity']
  ]
};

export function DashboardLayout({ role }) {
  const { user, logout } = useAuth();
  const nav = useNavigate();
  const [open, setOpen] = useState(false);
  return (
    <>
      <Navbar />
      <div className="dash">
        <aside className={`sidebar ${open ? 'open' : ''}`}>
          <div className="side-user">
            <div className="avatar">{user.fullName[0]}</div>
            <div><strong>{user.fullName}</strong><div className="small muted">{role}</div></div>
          </div>
          {MENUS[role].map(([to, icon, label, end]) => (
            <NavLink key={to} to={to} end={!!end} onClick={() => setOpen(false)}>{icon} {label}</NavLink>
          ))}
          <button className="side-logout" onClick={() => { logout(); nav('/'); }}>🚪 Logout</button>
        </aside>
        <section className="dash-main">
          <button className="btn btn-outline btn-sm side-toggle" onClick={() => setOpen(!open)}>☰ Menu</button>
          <Outlet />
        </section>
      </div>
    </>
  );
}

// Only lets the right role in. The server ALSO enforces roles; this is just for navigation.
export function ProtectedRoute({ role }) {
  const { user } = useAuth();
  const loc = useLocation();
  if (!user) return <Navigate to="/login" state={{ from: loc.pathname }} replace />;
  if (user.role !== role) return <Navigate to={homeFor(user.role)} replace />;
  return <DashboardLayout role={role} />;
}
