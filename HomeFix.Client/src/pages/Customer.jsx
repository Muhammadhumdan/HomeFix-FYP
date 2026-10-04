import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { errMsg } from '../api';
import { useAuth } from '../auth.jsx';
import { Alert, Async, Empty, PageHead, StatCard, Stars, StatusBadge, fmtDate, money, useFetch } from '../ui.jsx';

const STATUSES = ['All', 'Pending', 'Accepted', 'Completed', 'Rejected', 'Cancelled'];

export function UserProfileForm() {
  const { updateUser } = useAuth();
  const { data, loading, error } = useFetch(() => api.get('/users/me'));
  const [f, setF] = useState(null);
  const [msg, setMsg] = useState({ type: '', text: '' });
  const form = f || data;
  const set = (k) => (e) => setF({ ...form, [k]: e.target.value });
  const save = async (e) => {
    e.preventDefault(); setMsg({});
    try { const r = await api.put('/users/me', form); updateUser(r.data); setF(r.data); setMsg({ type: 'success', text: 'Profile saved.' }); }
    catch (err) { setMsg({ type: 'error', text: errMsg(err) }); }
  };
  return (
    <Async loading={loading} error={error}>
      {form && (
        <form className="card" onSubmit={save}>
          <h3>Personal details</h3>
          <Alert type={msg.type}>{msg.text}</Alert>
          <div className="form-row">
            <div><label>Full name</label><input required value={form.fullName} onChange={set('fullName')} /></div>
            <div><label>Email</label><input disabled value={form.email} /></div>
            <div><label>Phone</label><input value={form.phone || ''} onChange={set('phone')} /></div>
            <div><label>City</label><input value={form.city || ''} onChange={set('city')} /></div>
          </div>
          <label>Address</label><input value={form.address || ''} onChange={set('address')} />
          <button className="btn btn-primary mt">Save changes</button>
        </form>
      )}
    </Async>
  );
}

export function CustomerProfile() {
  return <><PageHead title="My Profile" subtitle="Keep your contact details up to date" /><UserProfileForm /></>;
}

export function BookingCard({ b, actions, to }) {
  return (
    <div className="card booking-card">
      <div>
        <div className="row"><h3 style={{ margin: 0 }}>{b.serviceTitle}</h3><StatusBadge status={b.status} /></div>
        <div className="small muted">{b.providerName} • {b.customerName}</div>
        <div className="small">📅 {fmtDate(b.scheduledDate)} • 📍 {b.address} • {money(b.estimatedPrice)} / {b.priceUnit}</div>
      </div>
      <div className="actions">{to && <Link className="btn btn-outline btn-sm" to={to}>Details</Link>}{actions}</div>
    </div>
  );
}

export function CustomerDashboard() {
  const { user } = useAuth();
  const { data, loading, error } = useFetch(() => api.get('/bookings'));
  const count = (s) => data?.filter((b) => b.status === s).length || 0;
  const toReview = data?.filter((b) => b.canReview) || [];
  return (
    <>
      <PageHead title={`Welcome, ${user.fullName.split(' ')[0]} 👋`} subtitle="Here is what is happening with your bookings">
        <Link to="/customer/ai-search" className="btn btn-primary">✨ Find a provider</Link>
      </PageHead>
      <Async loading={loading} error={error}>
        <div className="grid g4">
          <StatCard icon="📅" label="Total bookings" value={data?.length || 0} />
          <StatCard icon="⏳" label="Pending" value={count('Pending')} />
          <StatCard icon="✅" label="Accepted" value={count('Accepted')} />
          <StatCard icon="🏁" label="Completed" value={count('Completed')} />
        </div>
        {toReview.length > 0 && (
          <div className="mt"><Alert type="info">You have {toReview.length} completed job(s) waiting for your review.</Alert>
            {toReview.map((b) => <BookingCard key={b.id} b={b} to={`/customer/bookings/${b.id}`} actions={<Link className="btn btn-accent btn-sm" to={`/customer/review/${b.id}`}>⭐ Review</Link>} />)}
          </div>
        )}
        <h2 className="mt">Recent bookings</h2>
        {data?.length === 0 ? <Empty title="No bookings yet" text="Find a provider and book your first service." action={<Link className="btn btn-primary" to="/customer/search">Browse providers</Link>} />
          : <div className="grid">{data?.slice(0, 4).map((b) => <BookingCard key={b.id} b={b} to={`/customer/bookings/${b.id}`} />)}</div>}
      </Async>
    </>
  );
}

export function MyBookings() {
  const [tab, setTab] = useState('All');
  const { data, loading, error, reload } = useFetch(() => api.get('/bookings'));
  const [err, setErr] = useState('');
  const cancel = async (id) => {
    if (!window.confirm('Cancel this booking?')) return;
    try { await api.post(`/bookings/${id}/cancel`); setErr(''); reload(); } catch (e) { setErr(errMsg(e)); }
  };
  const list = data?.filter((b) => tab === 'All' || b.status === tab) || [];
  return (
    <>
      <PageHead title="My Bookings" subtitle="Track the status of your service requests" />
      <div className="tabs">{STATUSES.map((s) => <button key={s} className={`tab ${tab === s ? 'on' : ''}`} onClick={() => setTab(s)}>{s}</button>)}</div>
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        {list.length === 0 ? <Empty title="No bookings here" /> : (
          <div className="grid">{list.map((b) => (
            <BookingCard key={b.id} b={b} to={`/customer/bookings/${b.id}`} actions={<>
              {['Pending', 'Accepted'].includes(b.status) && <button className="btn btn-danger btn-sm" onClick={() => cancel(b.id)}>Cancel</button>}
              {b.canReview && <Link className="btn btn-accent btn-sm" to={`/customer/review/${b.id}`}>⭐ Review</Link>}
            </>} />
          ))}</div>
        )}
      </Async>
    </>
  );
}

const FLOW = ['Pending', 'Accepted', 'Completed'];
export function BookingDetails() {
  const { id } = useParams();
  const { data: b, loading, error, reload } = useFetch(() => api.get(`/bookings/${id}`), [id]);
  const [err, setErr] = useState('');
  const cancel = async () => {
    if (!window.confirm('Cancel this booking?')) return;
    try { await api.post(`/bookings/${id}/cancel`); reload(); } catch (e) { setErr(errMsg(e)); }
  };
  return (
    <>
      <PageHead title="Booking Details"><Link to="/customer/bookings" className="btn btn-outline btn-sm">← Back</Link></PageHead>
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        {b && (
          <div className="card">
            <div className="spread"><h2>{b.serviceTitle}</h2><StatusBadge status={b.status} /></div>
            <div className="row mt">
              {['Rejected', 'Cancelled'].includes(b.status) ? <span className="chip chip-gray">This booking is {b.status.toLowerCase()}</span>
                : FLOW.map((s, i) => <span key={s} className={`chip ${FLOW.indexOf(b.status) >= i ? 'chip-green' : 'chip-gray'}`}>{i + 1}. {s}</span>)}
            </div>
            <dl className="detail-grid mt">
              <div><dt>Provider</dt><dd><Link to={`/providers/${b.providerId}`}>{b.providerName}</Link></dd></div>
              <div><dt>Category</dt><dd>{b.categoryName}</dd></div>
              <div><dt>Date</dt><dd>{fmtDate(b.scheduledDate)}</dd></div>
              <div><dt>Address</dt><dd>{b.address}</dd></div>
              <div><dt>Estimated price</dt><dd>{money(b.estimatedPrice)} / {b.priceUnit}</dd></div>
              <div><dt>Provider phone</dt><dd>{b.providerPhone || 'Shared after acceptance'}</dd></div>
              <div><dt>Requested on</dt><dd>{fmtDate(b.createdAt)}</dd></div>
              <div><dt>Notes</dt><dd>{b.notes || '—'}</dd></div>
            </dl>
            <div className="row mt">
              {['Pending', 'Accepted'].includes(b.status) && <button className="btn btn-danger" onClick={cancel}>Cancel booking</button>}
              {b.canReview && <Link className="btn btn-accent" to={`/customer/review/${b.id}`}>⭐ Write a review</Link>}
            </div>
            {b.review && (
              <div className="mt"><h3>Your review</h3><Stars value={b.review.rating} /><p>{b.review.comment}</p></div>
            )}
          </div>
        )}
      </Async>
    </>
  );
}

export function ReviewPage() {
  const { bookingId } = useParams();
  const nav = useNavigate();
  const { data: b, loading, error } = useFetch(() => api.get(`/bookings/${bookingId}`), [bookingId]);
  const [rating, setRating] = useState(0);
  const [hover, setHover] = useState(0);
  const [comment, setComment] = useState('');
  const [err, setErr] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e) => {
    e.preventDefault(); setErr('');
    if (!rating) return setErr('Please choose a star rating.');
    setBusy(true);
    try { await api.post('/reviews', { bookingId: Number(bookingId), rating, comment }); nav(`/customer/bookings/${bookingId}`); }
    catch (e2) { setErr(errMsg(e2)); setBusy(false); }
  };
  return (
    <>
      <PageHead title="Rate & Review" />
      <Async loading={loading} error={error}>
        {b && (b.status !== 'Completed' ? <Alert type="warn">Only completed bookings can be reviewed.</Alert> : b.review ? <Alert type="info">You already reviewed this booking.</Alert> : (
          <form className="card form-wide" onSubmit={submit}>
            <h3>{b.serviceTitle}</h3><p className="muted">with {b.providerName}</p>
            <Alert>{err}</Alert>
            <label>Your rating</label>
            <div className="star-input">{[1, 2, 3, 4, 5].map((n) => (
              <span key={n} className={n <= (hover || rating) ? 'on' : ''} onMouseEnter={() => setHover(n)} onMouseLeave={() => setHover(0)} onClick={() => setRating(n)}>★</span>
            ))}</div>
            <label>Your review</label>
            <textarea required minLength={3} value={comment} onChange={(e) => setComment(e.target.value)} placeholder="How was the service?" />
            <button className="btn btn-primary mt" disabled={busy}>{busy ? 'Submitting…' : 'Submit review'}</button>
          </form>
        ))}
      </Async>
    </>
  );
}
