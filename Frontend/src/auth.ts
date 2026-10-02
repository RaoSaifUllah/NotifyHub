export type Session = { sessionId: string };
export type Challenge = { status: 'EnrollMfa' | 'VerifyMfa'; challengeToken: string; csrfToken: string; authenticatorKey?: string };
type Authenticated = { status: 'Authenticated'; accessToken: string; csrfToken: string; sessionId: string; recoveryCodes: string[] };
export class AuthError extends Error { constructor(public code: string) { super(code); } }
let accessToken: string | null = null;
let csrfInMemory = '';
let accessExpires = 0;
let snapshot: Session | null = null;
let refreshPromise: Promise<void> | null = null;
const listeners = new Set<() => void>();
const channel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('notifyhub-session') : null;
export const subscribeSession = (listener: () => void) => { listeners.add(listener); return () => { listeners.delete(listener); }; };
export const sessionSnapshot = () => snapshot;
function notify() { listeners.forEach(listener => listener()); }
function clear() { accessToken = null; csrfInMemory = ''; accessExpires = 0; snapshot = null; notify(); }
if (channel) channel.onmessage = () => clear();
function csrfCookie() {
  return document.cookie.split('; ').find(value => value.startsWith('__Host-notifyhub-csrf='))?.split('=').slice(1).join('=') ?? '';
}
async function post(path: string, body?: unknown, csrf?: string): Promise<Record<string, unknown> | null> {
  if (location.protocol !== 'https:') throw new AuthError('ORIGIN_DENIED');
  let response: Response;
  try {
    response = await fetch('/api/v1/auth/' + path, {
      method: 'POST', credentials: 'same-origin', signal: AbortSignal.timeout(10_000),
      headers: { Accept: 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}), ...(csrf ? { 'X-CSRF-Token': csrf } : {}) },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch { throw new AuthError('NETWORK'); }
  if (!response.ok) throw new AuthError(response.status === 429 ? 'RATE_LIMITED' :
    response.status === 503 ? 'NOT_CONFIGURED' : response.status === 403 ? 'ORIGIN_DENIED' : 'LOGIN_FAILED');
  if (response.status === 204) return null;
  try {
    const result: unknown = await response.json();
    if (!result || typeof result !== 'object' || Array.isArray(result)) throw Error();
    return result as Record<string, unknown>;
  } catch { throw new AuthError('INVALID_RESPONSE'); }
}
function authenticated(body: Record<string, unknown> | null): Authenticated {
  if (!body || typeof body.accessToken !== 'string' || !body.accessToken || body.accessToken.length > 8192 ||
    typeof body.csrfToken !== 'string' || body.csrfToken.length !== 43 ||
    typeof body.sessionId !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(body.sessionId) ||
    (body.recoveryCodes !== undefined && (!Array.isArray(body.recoveryCodes) || body.recoveryCodes.length > 10 ||
      body.recoveryCodes.some(code => typeof code !== 'string' || code.length !== 43)))) throw new AuthError('INVALID_RESPONSE');
  return { status: 'Authenticated', accessToken: body.accessToken, csrfToken: body.csrfToken,
    sessionId: body.sessionId, recoveryCodes: (body.recoveryCodes ?? []) as string[] };
}
function adopt(result: Authenticated) {
  accessToken = result.accessToken;
  csrfInMemory = result.csrfToken;
  accessExpires = Date.now() + 285_000;
  snapshot = { sessionId: result.sessionId };
  notify();
}
export async function login(email: string, password: string): Promise<Challenge | Authenticated> {
  const body = await post('login', { email, password });
  if (body?.status === 'Authenticated') {
    const result = authenticated(body); adopt(result); channel?.postMessage('changed'); return result;
  }
  if (!body || (body.status !== 'EnrollMfa' && body.status !== 'VerifyMfa') ||
    typeof body.challengeToken !== 'string' || body.challengeToken.length !== 43 ||
    typeof body.csrfToken !== 'string' || body.csrfToken.length !== 43 ||
    (body.status === 'EnrollMfa' && (typeof body.authenticatorKey !== 'string' || !/^[A-Z2-7]{32}$/.test(body.authenticatorKey))))
    throw new AuthError('INVALID_RESPONSE');
  return { status: body.status, challengeToken: body.challengeToken, csrfToken: body.csrfToken,
    authenticatorKey: typeof body.authenticatorKey === 'string' ? body.authenticatorKey : undefined };
}
export async function completeMfa(challenge: Challenge, code: string, recovery: boolean): Promise<string[]> {
  const result = authenticated(await post('mfa/complete', { challengeToken: challenge.challengeToken, code, recovery }, challenge.csrfToken));
  adopt(result); channel?.postMessage('changed'); return result.recoveryCodes;
}
async function serialize<T>(action: () => Promise<T>): Promise<T> {
  if (!navigator.locks) throw new AuthError('BROWSER_UNSUPPORTED');
  return navigator.locks.request('notifyhub-refresh', { signal: AbortSignal.timeout(10_000) }, action);
}
export async function restoreSession(): Promise<void> {
  if (!csrfCookie()) return;
  if (!refreshPromise) refreshPromise = serialize(async () => {
    try { adopt(authenticated(await post('refresh', undefined, csrfCookie()))); }
    catch (error) { clear(); throw error; }
  }).finally(() => { refreshPromise = null; });
  return refreshPromise;
}
export async function bearerToken(): Promise<string> {
  if (!accessToken || Date.now() >= accessExpires) await restoreSession();
  if (!accessToken) throw new AuthError('LOGIN_FAILED');
  return accessToken;
}
export async function logout(): Promise<void> {
  await serialize(async () => {
    await post('logout', undefined, csrfCookie() || csrfInMemory);
    clear(); channel?.postMessage('revoked');
  });
}