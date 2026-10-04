import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { errMsg } from './api';

export const money = (n) => `Rs. ${Number(n || 0).toLocaleString('en-PK')}`;
export const fmtDate = (d) => (d ? new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }) : '');
export const today = () => new Date().toISOString().slice(0, 10);
export const tomorrow = () => new Date(Date.now() + 86400000).toISOString().slice(0, 10);

// Loads data from the API and exposes loading / error / reload
export function useFetch(fn, deps = []) {
  const [state, setState] = useState({ data: null, loading: true, error: '' });
  const [tick, setTick] = useState(0);
  useEffect(() => {
    let alive = true;
    setState((s) => ({ ...s, loading: true, error: '' }));
    fn()
      .then((r) => alive && setState({ data: r.data, loading: false, error: '' }))
      .catch((e) => alive && setState({ data: null, loading: false, error: errMsg(e) }));
    return () => { alive = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick]);
  return { ...state, reload: () => setTick((t) => t + 1) };
}

export const Loader = ({ text = 'Loading…' }) => <div className="loader"><span className="spinner" /> {text}</div>;
export const Alert = ({ type = 'error', children }) => (children ? <div className={`alert alert-${type}`}>{children}</div> : null);
export const Empty = ({ title, text, action }) => (
  <div className="empty"><div className="empty-icon">📭</div><h3>{title}</h3>{text && <p>{text}</p>}{action}</div>
);

export function Async({ loading, error, children }) {
  if (loading) return <Loader />;
  if (error) return <Alert>{error}</Alert>;
  return children;
}

export const Stars = ({ value = 0, size = 16 }) => (
  <span className="stars" style={{ fontSize: size }} title={`${value} / 5`}>
    {[1, 2, 3, 4, 5].map((i) => <span key={i} className={i <= Math.round(value) ? 'on' : 'off'}>★</span>)}
  </span>
);

export const StatusBadge = ({ status }) => <span className={`badge badge-${String(status).toLowerCase()}`}>{status}</span>;

export function Modal({ title, onClose, children }) {
  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-head"><h3>{title}</h3><button className="btn-link" onClick={onClose}>✕</button></div>
        {children}
      </div>
    </div>
  );
}

export const PageHead = ({ title, subtitle, children }) => (
  <div className="page-head"><div><h1>{title}</h1>{subtitle && <p className="muted">{subtitle}</p>}</div><div>{children}</div></div>
);

export const StatCard = ({ label, value, icon, tone = '' }) => (
  <div className={`stat ${tone}`}><div className="stat-icon">{icon}</div><div><div className="stat-value">{value}</div><div className="stat-label">{label}</div></div></div>
);

export function ProviderCard({ p }) {
  return (
    <div className="card provider-card">
      <div className="pc-top">
        <div className="avatar">{p.categoryIcon || '🛠️'}</div>
        <div>
          <h3>{p.businessName}</h3>
          <div className="muted small">{p.categoryName} • {p.serviceArea}</div>
        </div>
      </div>
      <div className="pc-rating"><Stars value={p.averageRating} /> <span className="small muted">{p.reviewCount ? `${p.averageRating} (${p.reviewCount})` : 'No reviews yet'}</span></div>
      <p className="small clamp">{p.bio}</p>
      <div className="pc-meta">
        <strong>{money(p.hourlyRate)}<span className="muted small">/hour</span></strong>
        <span className={`chip ${p.isAvailable ? 'chip-green' : 'chip-gray'}`}>{p.isAvailable ? 'Available' : 'Unavailable'}</span>
      </div>
      {p.services?.length > 0 && <div className="small muted">{p.services.slice(0, 2).map((s) => s.title).join(' • ')}</div>}
      <Link to={`/providers/${p.id}`} className="btn btn-primary btn-block">View profile</Link>
    </div>
  );
}
