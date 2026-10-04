import { useState } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import api, { errMsg } from '../api';
import { homeFor, useAuth } from '../auth.jsx';
import { Alert, useFetch } from '../ui.jsx';

const DEMO = [
  ['Admin', 'admin@homefix.com', 'Admin@123'], ['Customer', 'customer@homefix.com', 'Customer@123'],
  ['Electrician', 'electrician@homefix.com', 'Provider@123'], ['Plumber', 'plumber@homefix.com', 'Provider@123'],
  ['Painter', 'painter@homefix.com', 'Provider@123']
];

export function Login() {
  const { login } = useAuth();
  const nav = useNavigate();
  const loc = useLocation();
  const [params] = useSearchParams();
  const [f, setF] = useState({ email: '', password: '' });
  const [error, setError] = useState(params.get('expired') ? 'Your session expired. Please log in again.' : '');
  const [busy, setBusy] = useState(false);

  const submit = async (e) => {
    e.preventDefault(); setError(''); setBusy(true);
    try {
      const u = await login(f.email, f.password);
      nav(loc.state?.from && loc.state.from.startsWith(homeFor(u.role)) ? loc.state.from : homeFor(u.role), { replace: true });
    } catch (err) { setError(errMsg(err)); } finally { setBusy(false); }
  };

  return (
    <div className="container">
      <form className="card form-card" onSubmit={submit}>
        <h2>Welcome back</h2><p className="muted">Login to your HomeFix account</p>
        <Alert>{error}</Alert>
        <label>Email</label><input type="email" required value={f.email} onChange={(e) => setF({ ...f, email: e.target.value })} />
        <label>Password</label><input type="password" required value={f.password} onChange={(e) => setF({ ...f, password: e.target.value })} />
        <button className="btn btn-primary btn-block mt" disabled={busy}>{busy ? 'Logging in…' : 'Login'}</button>
        <p className="small center mt">New here? <Link to="/register">Create an account</Link></p>
        <hr />
        <div className="small muted">Demo accounts (click to fill):</div>
        <div className="demo">{DEMO.map(([n, e, p]) => <button type="button" key={n} className="chip chip-blue" style={{ border: 0, cursor: 'pointer' }} onClick={() => setF({ email: e, password: p })}>{n}</button>)}</div>
      </form>
    </div>
  );
}

export function Register() {
  const { register } = useAuth();
  const nav = useNavigate();
  const cats = useFetch(() => api.get('/categories'));
  const [f, setF] = useState({ role: 'Customer', fullName: '', email: '', password: '', phone: '', city: '', businessName: '', categoryId: '', serviceArea: '', hourlyRate: '', experienceYears: 0, bio: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value });
  const isProvider = f.role === 'Provider';

  const submit = async (e) => {
    e.preventDefault(); setError(''); setBusy(true);
    try {
      const payload = { ...f };
      if (isProvider) { payload.categoryId = Number(f.categoryId) || null; payload.hourlyRate = Number(f.hourlyRate) || null; payload.experienceYears = Number(f.experienceYears) || 0; }
      else { ['businessName', 'categoryId', 'serviceArea', 'hourlyRate', 'bio'].forEach((k) => delete payload[k]); delete payload.experienceYears; }
      const u = await register(payload);
      nav(homeFor(u.role), { replace: true });
    } catch (err) { setError(errMsg(err)); } finally { setBusy(false); }
  };

  return (
    <div className="container">
      <form className="card form-card form-wide" onSubmit={submit}>
        <h2>Create your account</h2>
        <div className="tabs mt">
          {['Customer', 'Provider'].map((r) => <button type="button" key={r} className={`tab ${f.role === r ? 'on' : ''}`} onClick={() => setF({ ...f, role: r })}>{r === 'Customer' ? '🙋 I need a service' : '🛠️ I provide services'}</button>)}
        </div>
        {isProvider && <Alert type="info">Provider accounts must be approved by an admin before you appear in search.</Alert>}
        <Alert>{error}</Alert>
        <div className="form-row">
          <div><label>Full name</label><input required value={f.fullName} onChange={set('fullName')} /></div>
          <div><label>Email</label><input type="email" required value={f.email} onChange={set('email')} /></div>
        </div>
        <div className="form-row">
          <div><label>Password (min 6)</label><input type="password" required minLength={6} value={f.password} onChange={set('password')} /></div>
          <div><label>Phone</label><input value={f.phone} onChange={set('phone')} placeholder="03xx-xxxxxxx" /></div>
          <div><label>City</label><input value={f.city} onChange={set('city')} placeholder="Kohat" /></div>
        </div>
        {isProvider && (
          <>
            <div className="form-row">
              <div><label>Business name</label><input required value={f.businessName} onChange={set('businessName')} /></div>
              <div><label>Service category</label>
                <select required value={f.categoryId} onChange={set('categoryId')}><option value="">Select…</option>{cats.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}</select></div>
            </div>
            <div className="form-row">
              <div><label>Service area(s)</label><input required value={f.serviceArea} onChange={set('serviceArea')} placeholder="Kohat, Hangu" /></div>
              <div><label>Hourly rate (Rs.)</label><input type="number" min="1" required value={f.hourlyRate} onChange={set('hourlyRate')} /></div>
              <div><label>Experience (years)</label><input type="number" min="0" value={f.experienceYears} onChange={set('experienceYears')} /></div>
            </div>
            <label>About your work</label><textarea value={f.bio} onChange={set('bio')} />
          </>
        )}
        <button className="btn btn-primary btn-block mt" disabled={busy}>{busy ? 'Creating…' : 'Register'}</button>
        <p className="small center mt">Already registered? <Link to="/login">Login</Link></p>
      </form>
    </div>
  );
}
