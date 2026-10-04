import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api from '../api';
import { useAuth } from '../auth.jsx';
import { Async, ProviderCard, useFetch } from '../ui.jsx';

const EXAMPLES = ['Find an electrician near Kohat', 'Need a plumber under Rs 3000', 'Looking for a painter'];

export default function Home() {
  const [q, setQ] = useState('');
  const nav = useNavigate();
  const { user } = useAuth();
  const cats = useFetch(() => api.get('/categories'));
  const featured = useFetch(() => api.get('/providers/featured?count=6'));
  const go = (text) => nav(`/ai-search?q=${encodeURIComponent(text)}`);

  return (
    <>
      <section className="hero">
        <div className="container">
          <h1>Find Trusted Local Services Near You</h1>
          <p>Electricians, plumbers, painters, carpenters, cleaners and mechanics — verified, rated and ready to book.</p>
          <form className="search-box" onSubmit={(e) => { e.preventDefault(); if (q.trim()) go(q.trim()); }}>
            <input value={q} onChange={(e) => setQ(e.target.value)} placeholder="What service do you need?" />
            <button className="btn btn-accent">✨ Search</button>
          </form>
          <div className="examples">
            {EXAMPLES.map((x) => <button key={x} className="example" onClick={() => go(x)}>{x}</button>)}
          </div>
          {!user && (
            <div className="row" style={{ marginTop: '1.4rem' }}>
              <Link to="/register" className="btn btn-accent">Get started</Link>
              <Link to="/login" className="btn btn-outline">Login</Link>
            </div>
          )}
        </div>
      </section>

      <section className="section">
        <div className="container">
          <h2>Popular Service Categories</h2><p className="sub">Pick a category to see verified providers</p>
          <Async loading={cats.loading} error={cats.error}>
            <div className="grid g4">
              {cats.data?.map((c) => (
                <Link key={c.id} to={`/search?categoryId=${c.id}`} className="card cat-card">
                  <div className="cat-icon">{c.icon}</div><h3>{c.name}</h3>
                  <div className="small muted">{c.providerCount} provider{c.providerCount === 1 ? '' : 's'}</div>
                </Link>
              ))}
            </div>
          </Async>
        </div>
      </section>

      <section className="section section-alt">
        <div className="container">
          <h2>How HomeFix Works</h2><p className="sub">Three simple steps</p>
          <div className="grid g3">
            {[['1', 'Search', 'Describe what you need in plain words or browse categories.'],
              ['2', 'Book', 'Choose a provider, compare rates and ratings, and send a booking request.'],
              ['3', 'Review', 'Once the job is done, rate your provider and help others decide.']].map(([n, t, d]) => (
              <div key={n} className="step"><div className="step-num">{n}</div><h3>{t}</h3><p className="muted">{d}</p></div>
            ))}
          </div>
        </div>
      </section>

      <section className="section">
        <div className="container">
          <h2>Featured Providers</h2><p className="sub">Top rated by customers</p>
          <Async loading={featured.loading} error={featured.error}>
            <div className="grid g3">{featured.data?.map((p) => <ProviderCard key={p.id} p={p} />)}</div>
          </Async>
        </div>
      </section>

      <section className="section section-alt">
        <div className="container">
          <h2>Why HomeFix?</h2><p className="sub">Built on trust</p>
          <div className="grid g4">
            {[['✅', 'Verified providers', 'Every provider is approved by an admin before appearing.'],
              ['⭐', 'Real reviews', 'Only customers with completed bookings can review.'],
              ['✨', 'AI-assisted search', 'Ask in plain language — we find the right provider.'],
              ['💰', 'Transparent rates', 'See hourly rates and service prices upfront.']].map(([i, t, d]) => (
              <div key={t} className="card center"><div className="cat-icon">{i}</div><h3>{t}</h3><p className="small muted">{d}</p></div>
            ))}
          </div>
        </div>
      </section>
    </>
  );
}
