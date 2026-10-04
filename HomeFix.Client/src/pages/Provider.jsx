import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { errMsg } from '../api';
import { Alert, Async, Empty, PageHead, StatCard, Stars, StatusBadge, fmtDate, money, useFetch } from '../ui.jsx';
import { BookingCard, UserProfileForm } from './Customer.jsx';

const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

function StatusBanner({ p }) {
  if (p.approvalStatus === 'Approved') return <Alert type="success">✅ Your profile is approved and visible to customers.</Alert>;
  if (p.approvalStatus === 'Rejected') return <Alert>❌ Your application was rejected: {p.rejectionReason}. Update your profile to resubmit.</Alert>;
  return <Alert type="warn">⏳ Your profile is awaiting admin approval. You are not visible to customers yet — you can still prepare your services.</Alert>;
}

export function ProviderDashboard() {
  const prof = useFetch(() => api.get('/providers/me'));
  const bk = useFetch(() => api.get('/bookings'));
  const n = (s) => bk.data?.filter((b) => b.status === s).length || 0;
  const p = prof.data;
  return (
    <>
      <PageHead title={p ? p.businessName : 'Provider Dashboard'} subtitle="Manage your services and bookings" />
      <Async loading={prof.loading || bk.loading} error={prof.error || bk.error}>
        {p && <StatusBanner p={p} />}
        <div className="grid g4">
          <StatCard icon="📥" label="New requests" value={n('Pending')} />
          <StatCard icon="🧰" label="Active jobs" value={n('Accepted')} />
          <StatCard icon="🏁" label="Completed" value={n('Completed')} />
          <StatCard icon="⭐" label={`${p?.reviewCount || 0} reviews`} value={p?.reviewCount ? p.averageRating : '—'} />
        </div>
        <h2 className="mt">Latest requests</h2>
        {n('Pending') === 0 ? <Empty title="No pending requests" /> : (
          <div className="grid">{bk.data?.filter((b) => b.status === 'Pending').slice(0, 3).map((b) => <BookingCard key={b.id} b={b} to="/provider/requests" />)}</div>
        )}
      </Async>
    </>
  );
}

export function ProviderProfilePage() {
  const { data, loading, error, reload } = useFetch(() => api.get('/providers/me'));
  const cats = useFetch(() => api.get('/categories'));
  const [f, setF] = useState(null);
  const [msg, setMsg] = useState({});
  useEffect(() => { if (data) setF({ businessName: data.businessName, bio: data.bio || '', categoryId: data.categoryId, serviceArea: data.serviceArea, hourlyRate: data.hourlyRate, experienceYears: data.experienceYears }); }, [data]);
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value });
  const save = async (e) => {
    e.preventDefault(); setMsg({});
    try { await api.put('/providers/me', { ...f, categoryId: Number(f.categoryId), hourlyRate: Number(f.hourlyRate), experienceYears: Number(f.experienceYears) }); setMsg({ type: 'success', text: 'Business profile saved.' }); reload(); }
    catch (err) { setMsg({ type: 'error', text: errMsg(err) }); }
  };
  return (
    <>
      <PageHead title="My Profile" subtitle="Your public business profile and personal details" />
      <Async loading={loading || cats.loading} error={error || cats.error}>
        {data && <StatusBanner p={data} />}
        {f && (
          <form className="card" onSubmit={save}>
            <h3>Business profile</h3><Alert type={msg.type}>{msg.text}</Alert>
            <div className="form-row">
              <div><label>Business name</label><input required value={f.businessName} onChange={set('businessName')} /></div>
              <div><label>Category</label><select value={f.categoryId} onChange={set('categoryId')}>{cats.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}</select></div>
              <div><label>Service area(s)</label><input required value={f.serviceArea} onChange={set('serviceArea')} placeholder="Kohat, Hangu" /></div>
              <div><label>Hourly rate (Rs.)</label><input type="number" min="1" required value={f.hourlyRate} onChange={set('hourlyRate')} /></div>
              <div><label>Experience (years)</label><input type="number" min="0" value={f.experienceYears} onChange={set('experienceYears')} /></div>
            </div>
            <label>About</label><textarea value={f.bio} onChange={set('bio')} />
            <button className="btn btn-primary mt">Save business profile</button>
          </form>
        )}
      </Async>
      <div className="mt"><UserProfileForm /></div>
    </>
  );
}

export function MyServices() {
  const { data, loading, error, reload } = useFetch(() => api.get('/services/mine'));
  const [err, setErr] = useState('');
  const del = async (id) => {
    if (!window.confirm('Delete this service?')) return;
    try { await api.delete(`/services/${id}`); reload(); } catch (e) { setErr(errMsg(e)); }
  };
  return (
    <>
      <PageHead title="My Services" subtitle="What you offer and your rates"><Link className="btn btn-primary" to="/provider/services/new">+ Add service</Link></PageHead>
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        {data?.length === 0 ? <Empty title="No services yet" text="Add your first service so customers can book you." /> : (
          <div className="table-wrap"><table>
            <thead><tr><th>Service</th><th>Category</th><th>Price</th><th>Status</th><th></th></tr></thead>
            <tbody>{data?.map((s) => (
              <tr key={s.id}><td><strong>{s.title}</strong><div className="small muted">{s.description}</div></td><td>{s.categoryName}</td>
                <td>{money(s.price)} / {s.priceUnit}</td><td><span className={`chip ${s.isActive ? 'chip-green' : 'chip-gray'}`}>{s.isActive ? 'Active' : 'Hidden'}</span></td>
                <td><div className="actions"><Link className="btn btn-outline btn-sm" to={`/provider/services/${s.id}/edit`}>Edit</Link><button className="btn btn-danger btn-sm" onClick={() => del(s.id)}>Delete</button></div></td></tr>
            ))}</tbody></table></div>
        )}
      </Async>
    </>
  );
}

export function ServiceForm() {
  const { id } = useParams();
  const nav = useNavigate();
  const [f, setF] = useState({ title: '', description: '', price: '', priceUnit: 'hour', isActive: true });
  const [err, setErr] = useState('');
  const [busy, setBusy] = useState(false);
  const existing = useFetch(() => (id ? api.get(`/services/mine/${id}`) : Promise.resolve({ data: null })), [id]);
  useEffect(() => { if (existing.data) setF({ ...existing.data }); }, [existing.data]);
  const set = (k) => (e) => setF({ ...f, [k]: e.target.value });
  const submit = async (e) => {
    e.preventDefault(); setErr(''); setBusy(true);
    const body = { title: f.title, description: f.description, price: Number(f.price), priceUnit: f.priceUnit, isActive: f.isActive };
    try { id ? await api.put(`/services/${id}`, body) : await api.post('/services', body); nav('/provider/services'); }
    catch (e2) { setErr(errMsg(e2)); setBusy(false); }
  };
  return (
    <>
      <PageHead title={id ? 'Edit Service' : 'Add Service'} />
      <Async loading={existing.loading} error={existing.error}>
        <form className="card form-wide" onSubmit={submit}>
          <Alert>{err}</Alert>
          <label>Service title</label><input required minLength={3} value={f.title} onChange={set('title')} placeholder="e.g. Fan & Switch Repair" />
          <label>Description</label><textarea value={f.description || ''} onChange={set('description')} />
          <div className="form-row">
            <div><label>Price (Rs.)</label><input type="number" min="1" required value={f.price} onChange={set('price')} /></div>
            <div><label>Charged per</label><select value={f.priceUnit} onChange={set('priceUnit')}><option value="hour">Hour</option><option value="day">Day</option><option value="job">Job</option></select></div>
          </div>
          <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}><input style={{ width: 'auto' }} type="checkbox" checked={f.isActive} onChange={(e) => setF({ ...f, isActive: e.target.checked })} /> Visible to customers</label>
          <div className="row mt"><button className="btn btn-primary" disabled={busy}>Save service</button><Link className="btn btn-outline" to="/provider/services">Cancel</Link></div>
        </form>
      </Async>
    </>
  );
}

export function Availability() {
  const { data, loading, error } = useFetch(() => api.get('/providers/me'));
  const [f, setF] = useState(null);
  const [msg, setMsg] = useState({});
  useEffect(() => { if (data) setF({ isAvailable: data.isAvailable, days: data.availableDays.split(','), startTime: data.startTime, endTime: data.endTime }); }, [data]);
  const toggle = (d) => setF({ ...f, days: f.days.includes(d) ? f.days.filter((x) => x !== d) : [...f.days, d] });
  const save = async (e) => {
    e.preventDefault(); setMsg({});
    try { await api.put('/providers/me/availability', { isAvailable: f.isAvailable, availableDays: f.days.join(','), startTime: f.startTime, endTime: f.endTime }); setMsg({ type: 'success', text: 'Availability updated.' }); }
    catch (err) { setMsg({ type: 'error', text: errMsg(err) }); }
  };
  return (
    <>
      <PageHead title="Availability" subtitle="Customers can only book you on the days you choose" />
      <Async loading={loading} error={error}>
        {f && (
          <form className="card form-wide" onSubmit={save}>
            <Alert type={msg.type}>{msg.text}</Alert>
            <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}><input style={{ width: 'auto' }} type="checkbox" checked={f.isAvailable} onChange={(e) => setF({ ...f, isAvailable: e.target.checked })} /> I am currently accepting bookings</label>
            <label>Working days</label>
            <div className="checks">{DAYS.map((d) => <label key={d}><input type="checkbox" checked={f.days.includes(d)} onChange={() => toggle(d)} /> {d}</label>)}</div>
            <div className="form-row">
              <div><label>Start time</label><input type="time" required value={f.startTime} onChange={(e) => setF({ ...f, startTime: e.target.value })} /></div>
              <div><label>End time</label><input type="time" required value={f.endTime} onChange={(e) => setF({ ...f, endTime: e.target.value })} /></div>
            </div>
            <button className="btn btn-primary mt">Save availability</button>
          </form>
        )}
      </Async>
    </>
  );
}

function useAction(reload) {
  const [err, setErr] = useState('');
  const act = async (id, action) => {
    try { await api.post(`/bookings/${id}/${action}`); setErr(''); reload(); } catch (e) { setErr(errMsg(e)); }
  };
  return [err, act];
}

export function BookingRequests() {
  const { data, loading, error, reload } = useFetch(() => api.get('/bookings?status=Pending'));
  const [err, act] = useAction(reload);
  return (
    <>
      <PageHead title="Booking Requests" subtitle="Accept or reject incoming requests" />
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        {data?.length === 0 ? <Empty title="No pending requests" /> : (
          <div className="grid">{data?.map((b) => (
            <BookingCard key={b.id} b={b} actions={<>
              <button className="btn btn-success btn-sm" onClick={() => act(b.id, 'accept')}>Accept</button>
              <button className="btn btn-danger btn-sm" onClick={() => act(b.id, 'reject')}>Reject</button></>} />
          ))}</div>
        )}
      </Async>
    </>
  );
}

export function Jobs() {
  const [tab, setTab] = useState('Accepted');
  const { data, loading, error, reload } = useFetch(() => api.get('/bookings'));
  const [err, act] = useAction(reload);
  const list = data?.filter((b) => (tab === 'History' ? ['Completed', 'Rejected', 'Cancelled'].includes(b.status) : b.status === tab)) || [];
  return (
    <>
      <PageHead title="Jobs" subtitle="Active jobs and your previous work" />
      <div className="tabs">{[['Accepted', 'Active jobs'], ['Completed', 'Completed'], ['History', 'All history']].map(([k, l]) => <button key={k} className={`tab ${tab === k ? 'on' : ''}`} onClick={() => setTab(k)}>{l}</button>)}</div>
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        {list.length === 0 ? <Empty title="Nothing here yet" /> : (
          <div className="grid">{list.map((b) => (
            <div key={b.id}>
              <BookingCard b={b} actions={b.status === 'Accepted' && <button className="btn btn-success btn-sm" onClick={() => act(b.id, 'complete')}>Mark completed</button>} />
              {b.customerPhone && <div className="small muted" style={{ padding: '.3rem 1rem' }}>📞 Customer phone: {b.customerPhone}</div>}
              {b.review && <div className="small" style={{ padding: '.3rem 1rem' }}><Stars value={b.review.rating} size={13} /> “{b.review.comment}”</div>}
            </div>
          ))}</div>
        )}
      </Async>
    </>
  );
}

export function ProviderReviews() {
  const { data, loading, error } = useFetch(() => api.get('/reviews/mine'));
  const avg = data?.length ? (data.reduce((a, r) => a + r.rating, 0) / data.length).toFixed(1) : null;
  return (
    <>
      <PageHead title="My Reviews" subtitle={avg ? `Average rating ${avg} from ${data.length} review(s)` : 'Customer feedback'} />
      <Async loading={loading} error={error}>
        {data?.length === 0 ? <Empty title="No reviews yet" text="Complete jobs to receive reviews." /> : (
          <div className="card">{data?.map((r) => (
            <div key={r.id} className="review"><div className="spread"><strong>{r.customerName}</strong><span className="small muted">{fmtDate(r.createdAt)}</span></div>
              <Stars value={r.rating} size={14} /> <span className="small muted">{r.serviceTitle}</span><p>{r.comment}</p></div>
          ))}</div>
        )}
      </Async>
    </>
  );
}
