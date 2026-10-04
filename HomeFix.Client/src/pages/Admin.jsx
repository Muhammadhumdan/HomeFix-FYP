import { useState } from 'react';
import api, { errMsg } from '../api';
import { Alert, Async, Empty, Modal, PageHead, StatCard, StatusBadge, fmtDate, money, useFetch } from '../ui.jsx';

export function AdminDashboard() {
  const { data: s, loading, error } = useFetch(() => api.get('/admin/stats'));
  const act = useFetch(() => api.get('/admin/activity'));
  return (
    <>
      <PageHead title="Admin Dashboard" subtitle="Platform overview" />
      <Async loading={loading} error={error}>
        {s && (
          <>
            <div className="grid g4">
              <StatCard icon="🙋" label="Total customers" value={s.totalCustomers} />
              <StatCard icon="🛠️" label="Total providers" value={s.totalProviders} />
              <StatCard icon="⏳" label="Pending approvals" value={s.pendingApprovals} />
              <StatCard icon="🧾" label="Total services" value={s.totalServices} />
              <StatCard icon="📅" label="Total bookings" value={s.totalBookings} />
              <StatCard icon="🏁" label="Completed bookings" value={s.completedBookings} />
              <StatCard icon="⭐" label="Reviews" value={s.totalReviews} />
              <StatCard icon="🚩" label="Open reports" value={s.openReports} />
            </div>
            <h2 className="mt">Bookings by status</h2>
            <div className="row">{Object.entries(s.bookingsByStatus).map(([k, v]) => <span key={k} className="row"><StatusBadge status={k} /> <strong>{v}</strong>&nbsp;</span>)}</div>
          </>
        )}
      </Async>
      <h2 className="mt">Recent activity</h2>
      <Async loading={act.loading} error={act.error}><ActivityList items={act.data?.slice(0, 8)} /></Async>
    </>
  );
}

function ActivityList({ items }) {
  const icon = { User: '👤', Booking: '📅', Review: '⭐', Report: '🚩' };
  if (!items?.length) return <Empty title="No activity yet" />;
  return <div className="card">{items.map((a, i) => <div key={i} className="review spread"><span>{icon[a.type]} {a.message}</span><span className="small muted">{fmtDate(a.at)}</span></div>)}</div>;
}

export function Approvals() {
  const [tab, setTab] = useState('Pending');
  const { data, loading, error, reload } = useFetch(() => api.get('/admin/providers', { params: { status: tab } }), [tab]);
  const [rej, setRej] = useState(null);
  const [reason, setReason] = useState('');
  const [err, setErr] = useState('');
  const approve = async (id) => { try { await api.post(`/admin/providers/${id}/approve`); setErr(''); reload(); } catch (e) { setErr(errMsg(e)); } };
  const reject = async () => {
    try { await api.post(`/admin/providers/${rej.id}/reject`, { reason }); setRej(null); setReason(''); reload(); } catch (e) { setErr(errMsg(e)); }
  };
  return (
    <>
      <PageHead title="Provider Approvals" subtitle="Only approved providers are visible to customers" />
      <div className="tabs">{['Pending', 'Approved', 'Rejected'].map((t) => <button key={t} className={`tab ${tab === t ? 'on' : ''}`} onClick={() => setTab(t)}>{t}</button>)}</div>
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        {data?.length === 0 ? <Empty title={`No ${tab.toLowerCase()} providers`} /> : (
          <div className="table-wrap"><table>
            <thead><tr><th>Business</th><th>Owner</th><th>Category</th><th>Area</th><th>Rate</th><th>Status</th><th></th></tr></thead>
            <tbody>{data?.map((p) => (
              <tr key={p.id}><td><strong>{p.businessName}</strong><div className="small muted">{p.experienceYears} yrs experience</div></td><td>{p.providerName}</td><td>{p.categoryName}</td>
                <td>{p.serviceArea}</td><td>{money(p.hourlyRate)}/h</td><td><StatusBadge status={p.approvalStatus} /></td>
                <td><div className="actions">
                  {p.approvalStatus !== 'Approved' && <button className="btn btn-success btn-sm" onClick={() => approve(p.id)}>Approve</button>}
                  {p.approvalStatus !== 'Rejected' && <button className="btn btn-danger btn-sm" onClick={() => setRej(p)}>Reject</button>}
                </div></td></tr>
            ))}</tbody></table></div>
        )}
      </Async>
      {rej && (
        <Modal title={`Reject ${rej.businessName}`} onClose={() => setRej(null)}>
          <label>Reason (shown to the provider)</label><textarea value={reason} onChange={(e) => setReason(e.target.value)} />
          <button className="btn btn-danger btn-block mt" onClick={reject}>Reject provider</button>
        </Modal>
      )}
    </>
  );
}

export function Categories() {
  const { data, loading, error, reload } = useFetch(() => api.get('/categories/all'));
  const [edit, setEdit] = useState(null);
  const [err, setErr] = useState('');
  const [mErr, setMErr] = useState('');
  const save = async (e) => {
    e.preventDefault(); setMErr('');
    const body = { name: edit.name, description: edit.description, icon: edit.icon, isActive: edit.isActive };
    try { edit.id ? await api.put(`/categories/${edit.id}`, body) : await api.post('/categories', body); setEdit(null); reload(); } catch (e2) { setMErr(errMsg(e2)); }
  };
  const del = async (id) => {
    if (!window.confirm('Delete this category?')) return;
    try { await api.delete(`/categories/${id}`); setErr(''); reload(); } catch (e) { setErr(errMsg(e)); }
  };
  return (
    <>
      <PageHead title="Service Categories"><button className="btn btn-primary" onClick={() => { setMErr(''); setEdit({ name: '', description: '', icon: '🛠️', isActive: true }); }}>+ Add category</button></PageHead>
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        <div className="table-wrap"><table>
          <thead><tr><th></th><th>Name</th><th>Description</th><th>Providers</th><th>Services</th><th>Status</th><th></th></tr></thead>
          <tbody>{data?.map((c) => (
            <tr key={c.id}><td style={{ fontSize: '1.4rem' }}>{c.icon}</td><td><strong>{c.name}</strong></td><td className="small">{c.description}</td><td>{c.providerCount}</td><td>{c.serviceCount}</td>
              <td><span className={`chip ${c.isActive ? 'chip-green' : 'chip-gray'}`}>{c.isActive ? 'Active' : 'Inactive'}</span></td>
              <td><div className="actions"><button className="btn btn-outline btn-sm" onClick={() => { setMErr(''); setEdit(c); }}>Edit</button><button className="btn btn-danger btn-sm" onClick={() => del(c.id)}>Delete</button></div></td></tr>
          ))}</tbody></table></div>
      </Async>
      {edit && (
        <Modal title={edit.id ? 'Edit category' : 'New category'} onClose={() => setEdit(null)}>
          <Alert>{mErr}</Alert>
          <form onSubmit={save}>
            <label>Name</label><input required minLength={2} value={edit.name} onChange={(e) => setEdit({ ...edit, name: e.target.value })} />
            <label>Icon (emoji)</label><input maxLength={4} value={edit.icon || ''} onChange={(e) => setEdit({ ...edit, icon: e.target.value })} />
            <label>Description</label><textarea value={edit.description || ''} onChange={(e) => setEdit({ ...edit, description: e.target.value })} />
            <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}><input style={{ width: 'auto' }} type="checkbox" checked={edit.isActive} onChange={(e) => setEdit({ ...edit, isActive: e.target.checked })} /> Active</label>
            <button className="btn btn-primary btn-block mt">Save</button>
          </form>
        </Modal>
      )}
    </>
  );
}

export function Users() {
  const [role, setRole] = useState('');
  const { data, loading, error, reload } = useFetch(() => api.get('/admin/users', { params: { role: role || undefined } }), [role]);
  const [err, setErr] = useState('');
  const toggle = async (id) => { try { await api.post(`/admin/users/${id}/toggle-active`); setErr(''); reload(); } catch (e) { setErr(errMsg(e)); } };
  return (
    <>
      <PageHead title="Users" subtitle="Customers, providers and admins" />
      <div className="tabs">{[['', 'All'], ['Customer', 'Customers'], ['Provider', 'Providers'], ['Admin', 'Admins']].map(([k, l]) => <button key={k} className={`tab ${role === k ? 'on' : ''}`} onClick={() => setRole(k)}>{l}</button>)}</div>
      <Alert>{err}</Alert>
      <Async loading={loading} error={error}>
        <div className="table-wrap"><table>
          <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>City</th><th>Joined</th><th>Status</th><th></th></tr></thead>
          <tbody>{data?.map((u) => (
            <tr key={u.id}><td><strong>{u.fullName}</strong></td><td>{u.email}</td><td>{u.role}{u.providerStatus && <> <StatusBadge status={u.providerStatus} /></>}</td><td>{u.city}</td><td>{fmtDate(u.createdAt)}</td>
              <td><span className={`chip ${u.isActive ? 'chip-green' : 'chip-gray'}`}>{u.isActive ? 'Active' : 'Deactivated'}</span></td>
              <td>{u.role !== 'Admin' && <button className={`btn btn-sm ${u.isActive ? 'btn-danger' : 'btn-success'}`} onClick={() => toggle(u.id)}>{u.isActive ? 'Deactivate' : 'Activate'}</button>}</td></tr>
          ))}</tbody></table></div>
      </Async>
    </>
  );
}

export function AdminBookings() {
  const [st, setSt] = useState('');
  const { data, loading, error } = useFetch(() => api.get('/admin/bookings', { params: { status: st || undefined } }), [st]);
  return (
    <>
      <PageHead title="Bookings" subtitle="Monitor all bookings on the platform" />
      <div className="tabs">{['', 'Pending', 'Accepted', 'Completed', 'Rejected', 'Cancelled'].map((s) => <button key={s} className={`tab ${st === s ? 'on' : ''}`} onClick={() => setSt(s)}>{s || 'All'}</button>)}</div>
      <Async loading={loading} error={error}>
        {data?.length === 0 ? <Empty title="No bookings" /> : (
          <div className="table-wrap"><table>
            <thead><tr><th>#</th><th>Service</th><th>Customer</th><th>Provider</th><th>Date</th><th>Price</th><th>Status</th><th>Review</th></tr></thead>
            <tbody>{data?.map((b) => (
              <tr key={b.id}><td>{b.id}</td><td>{b.serviceTitle}</td><td>{b.customerName}</td><td>{b.providerName}</td><td>{fmtDate(b.scheduledDate)}</td><td>{money(b.estimatedPrice)}</td>
                <td><StatusBadge status={b.status} /></td><td>{b.review ? `${b.review.rating}★` : '—'}</td></tr>
            ))}</tbody></table></div>
        )}
      </Async>
    </>
  );
}

export function Activity() {
  const act = useFetch(() => api.get('/admin/activity'));
  const rep = useFetch(() => api.get('/admin/reports'));
  const [err, setErr] = useState('');
  const doIt = async (id, action) => { try { await api.post(`/admin/reports/${id}/${action}`); setErr(''); rep.reload(); } catch (e) { setErr(errMsg(e)); } };
  return (
    <>
      <PageHead title="Reports & Activity" subtitle="Reported users/content and recent platform events" />
      <h2>Reported users</h2><Alert>{err}</Alert>
      <Async loading={rep.loading} error={rep.error}>
        {rep.data?.length === 0 ? <Empty title="No reports" /> : (
          <div className="table-wrap"><table>
            <thead><tr><th>Reported user</th><th>Reported by</th><th>Reason</th><th>Date</th><th>Status</th><th></th></tr></thead>
            <tbody>{rep.data?.map((r) => (
              <tr key={r.id}><td><strong>{r.reportedUserName}</strong></td><td>{r.reporterName}</td><td>{r.reason}<div className="small muted">{r.details}{r.reviewComment && ` — Review: “${r.reviewComment}”`}</div></td>
                <td>{fmtDate(r.createdAt)}</td><td><StatusBadge status={r.status} /></td>
                <td>{r.status === 'Open' && <div className="actions"><button className="btn btn-success btn-sm" onClick={() => doIt(r.id, 'resolve')}>Resolve</button><button className="btn btn-outline btn-sm" onClick={() => doIt(r.id, 'dismiss')}>Dismiss</button></div>}</td></tr>
            ))}</tbody></table></div>
        )}
      </Async>
      <h2 className="mt">Platform activity</h2>
      <Async loading={act.loading} error={act.error}><ActivityList items={act.data} /></Async>
    </>
  );
}
