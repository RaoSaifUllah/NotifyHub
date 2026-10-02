import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { AuthError, login, completeMfa, type Challenge } from './auth';
import { t } from './en';

export function LoginPage() {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [code, setCode] = useState('');
  const [challenge, setChallenge] = useState<Challenge | null>(null);
  const [recovery, setRecovery] = useState(false);
  const [codes, setCodes] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => {
    if (challenge || codes.length) document.getElementById('auth-heading')?.focus();
  }, [challenge, codes.length]);
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError('');
    try {
      if (!challenge) {
        const result = await login(email, password);
        setPassword('');
        if (result.status === 'Authenticated') navigate('/');
        else setChallenge(result);
      } else {
        const recoveryCodes = await completeMfa(challenge, code.trim(), recovery);
        setCode('');
        if (recoveryCodes.length) { setCodes(recoveryCodes); setChallenge(null); }
        else navigate('/');
      }
    } catch (failure) {
      setError(t.authErrors[failure instanceof AuthError ? failure.code : 'NETWORK'] ?? t.authErrors.NETWORK);
    } finally { setBusy(false); }
  }
  return <main className="auth-layout">
    <div className="auth-story"><Link className="auth-brand" to="/">{t.name}</Link><span className="auth-orbit" aria-hidden="true">◎</span>
      <h2>{t.authStoryTitle}</h2><p>{t.authStoryDescription}</p><div className="auth-story-footer">{t.selfHosted}</div>
    </div>
    <section className="auth-card">
      <Link className="auth-back" to="/">{t.returnOverview}</Link>
      <p className="eyebrow">{t.secureAccess}</p>
      <h1 id="auth-heading" tabIndex={-1}>{codes.length ? t.saveRecovery : challenge ? challenge.status === 'EnrollMfa' ? t.setupMfa : t.verifyMfa : t.signIn}</h1>
      <p className="auth-intro">{codes.length ? t.recoveryDescription : challenge ? challenge.status === 'EnrollMfa' ? t.setupMfaDescription : t.verifyMfaDescription : t.signInDescription}</p>
      {error && <p className="auth-error" role="alert">{error}</p>}
      {codes.length ? <>
        <ul className="recovery-codes" aria-label={t.recoveryCodes}>{codes.map(value => <li key={value}><code>{value}</code></li>)}</ul>
        <p className="auth-note">{t.recoveryOnce}</p>
        <button className="primary-button" onClick={() => { setCodes([]); navigate('/'); }}>{t.savedContinue}</button>
      </> : <form onSubmit={submit} aria-busy={busy}>
        {!challenge ? <>
          <label htmlFor="email">{t.email}</label><input id="email" type="email" autoComplete="username" value={email} onChange={event => setEmail(event.target.value)} maxLength={254} required disabled={busy} />
          <label htmlFor="password">{t.password}</label><input id="password" type="password" autoComplete="current-password" value={password} onChange={event => setPassword(event.target.value)} maxLength={256} required disabled={busy} />
        </> : <>
          {challenge.status === 'EnrollMfa' && <div className="setup-key"><span>{t.authenticatorKey}</span><code>{challenge.authenticatorKey}</code><small>{t.authenticatorInstructions}</small></div>}
          <label htmlFor="verification-code">{recovery ? t.recoveryCode : t.verificationCode}</label>
          <input id="verification-code" type="text" autoComplete="one-time-code" inputMode={recovery ? 'text' : 'numeric'}
            value={code} onChange={event => setCode(event.target.value)} minLength={recovery ? 43 : 6}
            maxLength={recovery ? 43 : 6} pattern={recovery ? undefined : '[0-9]{6}'} required disabled={busy} />
          {challenge.status === 'VerifyMfa' && <button type="button" className="text-button" disabled={busy} onClick={() => { setRecovery(!recovery); setCode(''); setError(''); }}>{recovery ? t.useAuthenticator : t.useRecovery}</button>}
        </>}
        <button className="primary-button" type="submit" disabled={busy}>{busy ? t.signingIn : challenge ? t.verifyContinue : t.signIn}</button>
        {challenge && <button type="button" className="text-button" disabled={busy} onClick={() => { setChallenge(null); setCode(''); setRecovery(false); setError(''); }}>{t.startAgain}</button>}
      </form>}
      <p className="auth-note">{t.inviteOnly}</p>
    </section>
  </main>;
}