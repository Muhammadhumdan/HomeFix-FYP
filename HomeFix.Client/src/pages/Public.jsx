import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import api, { errMsg } from '../api';
import { useAuth } from '../auth.jsx';
import { Alert, Async, Empty, Loader, Modal, ProviderCard, Stars, fmtDate, money, today, tomorrow, useFetch } from '../ui.jsx';

export function About() {
  return (
    <div className="container section">
      <h1>About HomeFix</h1>
      <p>HomeFix is an AI-enhanced local services marketplace that connects customers with verified electricians, plumbers, painters, carpenters, cleaners and mechanics.</p>
      <div className="grid g3 mt">
        <div className="card"><h3>For customers</h3><p className="small muted">Search in plain language, compare rates and ratings, book and review.</p></div>
        <div className="card"><h3>For providers</h3><p className="small muted">Create a profile, list services, set rates and manage bookings.</p></div>
        <div className="card"><h3>For trust</h3><p className="small muted">Admins verify providers and monitor platform activity.</p></div>
      </div>
      <p className="small muted mt">BSSE Final Year Project — Institute of Computing, Kohat University of Science &amp; Technology.</p>
    </div>
  );
}

export function ServicesPage() {
  const [cat, setCat] = useState('');
  const [q, setQ] = useState('');
  const cats = useFetch(() => api.get('/categories'));
  const list = useFetch(() => api.get('/services', { params: { categoryId: cat || undefined, q: q || undefined } }), [cat, q]);
  return (
    <div className="container section">
      <h1>Browse Services</h1>
      <div className="tabs mt">
        <button className={`tab ${cat === '' ? 'on' : ''}`} onClick={() => setCat('')}>All</button>
        {cats.data?.map((c) => <button key={c.id} className={`tab ${cat === c.id ? 'on' : ''}`} onClick={() => setCat(c.id)}>{c.icon} {c.name}</button>)}
      </div>
      <input placeholder="Search services…" value={q} onChange={(e) => setQ(e.target.value)} style={{ maxWidth: 360, marginBottom: '1rem' }} />
      <Async loading={list.loading} error={list.error}>
        {list.data?.length === 0 ? <Empty title="No services found" text="Try another category or keyword." /> : (
          <div className="grid g2">
            {list.data?.map((s) => (
              <div key={s.id} className="card">
                <div className="spread"><h3>{s.title}</h3><span className="chip chip-blue">{s.categoryName}</span></div>
                <p className="small muted">{s.description}</p>
                <div className="spread"><strong>{money(s.price)} <span className="small muted">/ {s.priceUnit}</span></strong><Stars value={s.providerRating} size={14} /></div>
                <div className="small muted">by {s.providerName} • {s.serviceArea}</div>
                <Link className="btn btn-outline btn-sm mt" to={`/providers/${s.providerId}`}>View provider</Link>
              </div>
            ))}
          </div>
        )}
      </Async>
    </div>
  );
}

export function SearchPage() {
  const [params] = useSearchParams();
  const [f, setF] = useState({ q: params.get('q') || '', categoryId: params.get('categoryId') || '', location: '', minRate: '', maxRate: '', date: '', minRating: '', availableOnly: false });
  const [applied, setApplied] = useState(f);
  const cats = useFetch(() => api.get('/categories'));
  const res = useFetch(() => api.get('/providers', { params: Object.fromEntries(Object.entries(applied).filter(([, v]) => v !== '' && v !== false)) }), [applied]);
  const set = (k) => (e) => setF({ ...f, [k]: e.target.type === 'checkbox' ? e.target.checked : e.target.value });

  return (
    <div className="container section">
      <div className="spread"><h1>Search Providers</h1><Link to={window.location.pathname.startsWith('/customer') ? '/customer/ai-search' : '/ai-search'} className="btn btn-outline btn-sm">✨ Try AI search</Link></div>
      <form className="card mt" onSubmit={(e) => { e.preventDefault(); setApplied(f); }}>
        <div className="filters">
          <div><label>Keyword / provider / service</label><input value={f.q} onChange={set('q')} placeholder="e.g. wiring" /></div>
          <div><label>Category</label><select value={f.categoryId} onChange={set('categoryId')}><option value="">All</option>{cats.data?.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}</select></div>
          <div><label>Location</label><input value={f.location} onChange={set('location')} placeholder="Kohat" /></div>
          <div><label>Min rate</label><input type="number" min="0" value={f.minRate} onChange={set('minRate')} /></div>
          <div><label>Max rate</label><input type="number" min="0" value={f.maxRate} onChange={set('maxRate')} /></div>
          <div><label>Available on</label><input type="date" min={today()} value={f.date} onChange={set('date')} /></div>
          <div><label>Min rating</label><select value={f.minRating} onChange={set('minRating')}><option value="">Any</option>{[5, 4, 3].map((n) => <option key={n} value={n}>{n}+ stars</option>)}</select></div>
          <div><label style={{ display: 'flex', gap: 6, alignItems: 'center' }}><input style={{ width: 'auto' }} type="checkbox" checked={f.availableOnly} onChange={set('availableOnly')} /> Available now</label></div>
          <button className="btn btn-primary">Search</button>
        </div>
      </form>
      <div className="mt">
        <Async loading={res.loading} error={res.error}>
          <p className="muted small">{res.data?.length} provider(s) found</p>
          {res.data?.length === 0 ? <Empty title="No providers match" text="Try removing some filters." /> : <div className="grid g3">{res.data?.map((p) => <ProviderCard key={p.id} p={p} />)}</div>}
        </Async>
      </div>
    </div>
  );
}

export function AiSearchPage() {
  const [params] = useSearchParams();
  const [q, setQ] = useState(params.get('q') || '');
  const [state, setState] = useState({ loading: false, error: '', data: null });

  const run = async (text) => {
    if (!text.trim()) return;
    setState({ loading: true, error: '', data: null });
    try { setState({ loading: false, error: '', data: (await api.post('/search/ai', { query: text })).data }); }
    catch (e) { setState({ loading: false, error: errMsg(e), data: null }); }
  };
  useEffect(() => { if (params.get('q')) run(params.get('q')); /* eslint-disable-next-line */ }, []);
  const d = state.data;

  return (
    <div className="container section">
      <h1>✨ AI Search</h1><p className="muted">Describe what you need in your own words.</p>
      <form className="card" onSubmit={(e) => { e.preventDefault(); run(q); }}>
        <textarea value={q} onChange={(e) => setQ(e.target.value)} placeholder="I need an electrician near Kohat tomorrow under Rs 3000" />
        <div className="row mt">
          <button className="btn btn-primary" disabled={state.loading}>Search</button>
          {['I need an electrician near Kohat under 3000', 'plumber for a leaking tap in Kohat tomorrow', 'painter under Rs 5000'].map((x) => (
            <button type="button" key={x} className="chip chip-blue" style={{ border: 0, cursor: 'pointer' }} onClick={() => { setQ(x); run(x); }}>{x}</button>
          ))}
        </div>
      </form>
      {state.loading && <Loader text="Understanding your request…" />}
      <Alert>{state.error}</Alert>
      {d && (
        <div className="mt">
          <div className="parsed">
            <span className="chip chip-blue">Service: {d.parsed.serviceType || 'any'}</span>
            <span className="chip chip-blue">Location: {d.parsed.location || 'any'}</span>
            <span className="chip chip-blue">Budget: {d.parsed.maxBudget ? `up to ${money(d.parsed.maxBudget)}` : 'any'}</span>
            <span className="chip chip-blue">Date: {d.parsed.date ? fmtDate(d.parsed.date) : 'any'}</span>
            <span className="chip chip-gray">{d.usedAi ? '🤖 AI' : '⚙️'} {d.parser}</span>
          </div>
          {d.note && <Alert type="info">{d.note}</Alert>}
          {d.results.length === 0 ? <Empty title="No suitable providers found" text="Try changing the service, location or budget." /> : <div className="grid g3">{d.results.map((p) => <ProviderCard key={p.id} p={p} />)}</div>}
        </div>
      )}
    </div>
  );
}

export function ProviderDetails() {
  const { id } = useParams();
  const { user } = useAuth();
  const nav = useNavigate();
  const { data: p, loading, error } = useFetch(() => api.get(`/providers/${id}`), [id]);
  const [book, setBook] = useState(null);       // selected service
  const [report, setReport] = useState(false);

  return (
    <div className="container section">
      <Async loading={loading} error={error}>
        {p && (
          <>
            <div className="card">
              <div className="row"><div className="avatar" style={{ width: 64, height: 64, fontSize: '2rem' }}>{p.categoryIcon}</div>
                <div><h1>{p.businessName}</h1>
                  <div className="muted">{p.providerName} • {p.categoryName} • {p.serviceArea}</div>
                  <div className="row"><Stars value={p.averageRating} /><span className="small muted">{p.reviewCount ? `${p.averageRating} from ${p.reviewCount} review(s)` : 'No reviews yet'}</span></div></div></div>
              <p className="mt">{p.bio}</p>
              <div className="detail-grid">
                <div><dt>Hourly rate</dt><dd>{money(p.hourlyRate)}</dd></div>
                <div><dt>Experience</dt><dd>{p.experienceYears} years</dd></div>
                <div><dt>Availability</dt><dd>{p.isAvailable ? <span className="chip chip-green">Available</span> : <span className="chip chip-gray">Unavailable</span>}</dd></div>
                <div><dt>Working days</dt><dd>{p.availableDays.replaceAll(',', ', ')}</dd></div>
                <div><dt>Hours</dt><dd>{p.startTime} – {p.endTime}</dd></div>
              </div>
              {user?.role === 'Customer' && <button className="btn btn-outline btn-sm mt" onClick={() => setReport(true)}>🚩 Report provider</button>}
            </div>

            <h2 className="mt">Services</h2>
            {p.services.length === 0 ? <Empty title="No services listed yet" /> : (
              <div className="grid g2">
                {p.services.map((s) => (
                  <div key={s.id} className="card">
                    <h3>{s.title}</h3><p className="small muted">{s.description}</p>
                    <div className="spread"><strong>{money(s.price)} <span className="small muted">/ {s.priceUnit}</span></strong>
                      {user?.role === 'Customer' ? <button className="btn btn-primary btn-sm" disabled={!p.isAvailable} onClick={() => setBook(s)}>Book now</button>
                        : !user ? <Link to="/login" className="btn btn-outline btn-sm">Login to book</Link> : <span className="small muted">Customers can book</span>}
                    </div>
                  </div>
                ))}
              </div>
            )}

            <h2 className="mt">Reviews</h2>
            {p.reviews.length === 0 ? <Empty title="No reviews yet" /> : (
              <div className="card">{p.reviews.map((r) => (
                <div key={r.id} className="review"><div className="spread"><strong>{r.customerName}</strong><span className="small muted">{fmtDate(r.createdAt)}</span></div>
                  <Stars value={r.rating} size={14} /> <span className="small muted">for {r.serviceTitle}</span><p>{r.comment}</p></div>
              ))}</div>
            )}
          </>
        )}
      </Async>
      {book && <BookingModal service={book} provider={p} onClose={() => setBook(null)} onDone={(b) => nav(`/customer/bookings/${b.id}`)} />}
      {report && <ReportModal provider={p} onClose={() => setReport(false)} />}
    </div>
  );
}

function BookingModal({ service, provider, onClose, onDone }) {
  const { user } = useAuth();
  const [f, setF] = useState({ scheduledDate: tomorrow(), address: '', notes: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => { api.get('/users/me').then((r) => setF((x) => ({ ...x, address: r.data.address || '' }))).catch(() => {}); }, []);
  const submit = async (e) => {
    e.preventDefault(); setError(''); setBusy(true);
    try { onDone((await api.post('/bookings', { serviceId: service.id, ...f })).data); }
    catch (err) { setError(errMsg(err)); setBusy(false); }
  };
  return (
    <Modal title={`Book: ${service.title}`} onClose={onClose}>
      <p className="small muted">{provider.businessName} • {money(service.price)} / {service.priceUnit} • Works {provider.availableDays.replaceAll(',', ', ')}</p>
      <Alert>{error}</Alert>
      <form onSubmit={submit}>
        <label>Date</label><input type="date" required min={today()} value={f.scheduledDate} onChange={(e) => setF({ ...f, scheduledDate: e.target.value })} />
        <label>Service address</label><input required minLength={5} value={f.address} onChange={(e) => setF({ ...f, address: e.target.value })} placeholder="House, street, area" />
        <label>Notes (optional)</label><textarea value={f.notes} onChange={(e) => setF({ ...f, notes: e.target.value })} />
        <button className="btn btn-primary btn-block mt" disabled={busy}>{busy ? 'Sending…' : 'Send booking request'}</button>
      </form>
      <p className="small muted">Booking as {user.fullName}. The provider must accept your request.</p>
    </Modal>
  );
}

function ReportModal({ provider, onClose }) {
  const [f, setF] = useState({ reason: '', details: '' });
  const [msg, setMsg] = useState({ type: '', text: '' });
  const submit = async (e) => {
    e.preventDefault();
    try { await api.post('/reports', { reportedUserId: provider.userId, ...f }); setMsg({ type: 'success', text: 'Report sent. An admin will review it.' }); }
    catch (err) { setMsg({ type: 'error', text: errMsg(err) }); }
  };
  return (
    <Modal title="Report provider" onClose={onClose}>
      <Alert type={msg.type}>{msg.text}</Alert>
      <form onSubmit={submit}>
        <label>Reason</label><input required value={f.reason} onChange={(e) => setF({ ...f, reason: e.target.value })} placeholder="e.g. Unprofessional behaviour" />
        <label>Details</label><textarea value={f.details} onChange={(e) => setF({ ...f, details: e.target.value })} />
        <button className="btn btn-danger btn-block mt">Submit report</button>
      </form>
    </Modal>
  );
}
