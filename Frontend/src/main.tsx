import { BrowserRouter, Routes, Route, Link } from 'react-router';
import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { t } from './en';
import './styles.css';

function Icon({ name, size = 20 }: { name: 'bell' | 'grid' | 'sun' | 'moon' | 'arrow' | 'signal'; size?: number }) {
  const paths = {
    bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9" /><path d="M10 21h4" /></>,
    grid: <><rect x="3" y="3" width="7" height="7" rx="2" /><rect x="14" y="3" width="7" height="7" rx="2" /><rect x="3" y="14" width="7" height="7" rx="2" /><rect x="14" y="14" width="7" height="7" rx="2" /></>,
    sun: <><circle cx="12" cy="12" r="4" /><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1.5 1.5m11 11L19 19M5 19l1.5-1.5m11-11L19 5" /></>,
    moon: <path d="M21 13a9 9 0 0 1-10-10A9 9 0 1 0 21 13Z" />,
    arrow: <><path d="M5 12h14m-5-5 5 5-5 5" /></>,
    signal: <><path d="M4 18v3m5-7v7m5-11v11m5-16v16" /></>,
  };
  return <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">{paths[name]}</svg>;
}

function App() {
  const [dark, setDark] = useState(() => {
    try { const saved = localStorage.getItem('notifyhub-theme'); if (saved) return saved === 'dark'; } catch { /* Theme works without browser storage. */ }
    return matchMedia('(prefers-color-scheme: dark)').matches;
  });
  const [health, setHealth] = useState<'loading' | 'ready' | 'error'>('loading');
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    document.documentElement.dataset.theme = dark ? 'dark' : 'light';
    try { localStorage.setItem('notifyhub-theme', dark ? 'dark' : 'light'); } catch { /* Nonessential preference only. */ }
  }, [dark]);
  useEffect(() => {
    const controller = new AbortController();
    setHealth('loading');
    fetch('/health', { signal: controller.signal })
      .then(response => {
        if (!response.ok || !response.headers.get('content-type')?.includes('json')) throw Error();
        return response.json();
      })
      .then(() => setHealth('ready'))
      .catch(() => { if (!controller.signal.aborted) setHealth('error'); });
    return () => controller.abort();
  }, [revision]);
  return <>
    <a className="skip" href="#main">{t.skip}</a>
    <header>
      <a href="/" className="brand"><span className="brand-mark"><Icon name="bell" /></span>{t.name}</a>
      <div className="header-actions"><span className="header-caption">{t.tagline}</span>
        <button className="icon-button" onClick={() => setDark(!dark)} aria-label={t.theme}><Icon name={dark ? 'sun' : 'moon'} /></button>
      </div>
    </header>
    <div className="layout">
      <aside>
        <p className="nav-label">{t.workspace}</p>
        <nav aria-label={t.name}><a href="#main" aria-current="page"><Icon name="grid" />{t.overview}</a></nav>
        <div className="sidebar-note"><span className="small-mark"><Icon name="bell" size={16} /></span><p>{t.tagline}</p><small>{t.selfHosted}</small></div>
      </aside>
      <main id="main">
        <div className="page-heading"><div><p className="eyebrow">{t.overview}</p><h1>{t.title}</h1><p className="intro">{t.description}</p></div><span className="badge">{t.phase}</span></div>
        <section className="notice" aria-label={t.phase}><span className="notice-icon"><Icon name="bell" size={18} /></span><p>{t.status}</p></section>
        <div className="grid">
          <section className="panel connection-panel">
            <div className="card-heading"><span className="card-icon"><Icon name="signal" /></span><h2>{t.health}</h2></div>
            <p className={'connection-status ' + health} role="status"><span className="status-dot" />{health === 'loading' ? t.checking : health === 'ready' ? t.connected : t.unavailable}</p>
            {health === 'error' && <button className="secondary-button" onClick={() => setRevision(revision + 1)}>{t.retry}<Icon name="arrow" size={16} /></button>}
          </section>
          <section className="panel">
            <div className="card-heading"><span className="card-icon"><Icon name="arrow" /></span><h2>{t.channels}</h2></div>
            <p>{t.channelsDescription}</p><div className="channel-list">{t.channelNames.map(channel => <span key={channel}>{channel}</span>)}</div>
          </section>
        </div>
        <section className="panel activity-panel"><div className="activity-header"><h2>{t.activity}</h2><span className="subtle-badge">{t.noMessages}</span></div>
          <div className="empty">
            <div className="empty-art" aria-hidden="true"><span className="source-icon"><Icon name="grid" size={24} /></span><span className="connector" /><span className="hub-icon"><Icon name="bell" size={30} /></span><span className="connector" /><span className="source-icon"><Icon name="arrow" size={24} /></span></div>
            <h2>{t.noMessages}</h2><p>{t.noMessagesDescription}</p>
          </div>
        </section>
      </main>
    </div>
  </>;
}
function NotFoundPage() {
  return <main className="not-found"><span className="brand-mark"><Icon name="bell" /></span><h1>{t.notFound}</h1><p>{t.notFoundDescription}</p><Link to="/">{t.returnOverview}</Link></main>;
}
createRoot(document.getElementById('root')!).render(
  <React.StrictMode><BrowserRouter><Routes><Route path="/" element={<App />} /><Route path="*" element={<NotFoundPage />} /></Routes></BrowserRouter></React.StrictMode>
);

