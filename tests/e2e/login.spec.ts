import { test, expect } from '../../Frontend/node_modules/@playwright/test';
import AxeBuilder from '../../Frontend/node_modules/@axe-core/playwright';

const challenge = 'C'.repeat(43);
const csrf = 'S'.repeat(43);
const sessionId = '550e8400-e29b-41d4-a716-446655440000';
const codes = Array.from({ length: 10 }, (_, i) => String(i).repeat(43));
const access = 'test-memory-only-access-token';
async function health(page: import('../../Frontend/node_modules/@playwright/test').Page) {
  await page.route('**/health', route => route.fulfill({ contentType: 'application/json', body: '{"status":"healthy"}' }));
}
test('password to MFA enrollment, one-time recovery view and logout without token storage', async ({ page }) => {
  await health(page);
  await page.route('**/api/v1/auth/login', route => route.fulfill({ contentType: 'application/json',
    body: JSON.stringify({ status: 'EnrollMfa', challengeToken: challenge, csrfToken: csrf, authenticatorKey: 'A'.repeat(32) }) }));
  await page.route('**/api/v1/auth/mfa/complete', async route => {
    expect(route.request().headers()['x-csrf-token']).toBe(csrf);
    expect(route.request().postDataJSON()).toEqual({ challengeToken: challenge, code: '123456', recovery: false });
    await route.fulfill({ contentType: 'application/json', body: JSON.stringify({
      status: 'Authenticated', accessToken: access, csrfToken: csrf, sessionId, recoveryCodes: codes,
    }) });
  });
  await page.route('**/api/v1/auth/logout', route => route.fulfill({ status: 204 }));
  await page.goto('/login');
  await page.getByLabel('Email address').fill('admin@example.test');
  await page.getByLabel('Password', { exact: true }).fill('Example-Only-Password1!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Protect your account' })).toBeFocused();
  await expect(page.getByText('A'.repeat(32), { exact: true })).toBeVisible();
  await page.getByLabel('Six-digit code').fill('123456');
  await page.getByRole('button', { name: 'Verify and continue' }).click();
  await expect(page.getByRole('heading', { name: 'Save your recovery codes' })).toBeVisible();
  await expect(page.getByRole('list', { name: 'One-time recovery codes' }).getByRole('listitem')).toHaveCount(10);
  await page.getByRole('button', { name: 'I have saved my codes' }).click();
  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible();
  const stored = await page.evaluate(() => JSON.stringify({ local: { ...localStorage }, session: { ...sessionStorage } }));
  expect(stored).not.toContain(access);
  expect(stored).not.toContain(csrf);
  expect(stored).not.toContain(codes[0]);
  await expect(page.getByText(codes[0], { exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page.getByRole('link', { name: 'Sign in', exact: true })).toBeVisible();
});

test('MFA recovery switch and generic retry feedback', async ({ page }) => {
  await page.route('**/api/v1/auth/login', route => route.fulfill({ contentType: 'application/json',
    body: JSON.stringify({ status: 'VerifyMfa', challengeToken: challenge, csrfToken: csrf }) }));
  await page.route('**/api/v1/auth/mfa/complete', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: '{"code":"LOGIN_FAILED"}' }));
  await page.goto('/login');
  await page.getByLabel('Email address').fill('owner@example.test');
  await page.getByLabel('Password', { exact: true }).fill('Example-Only-Password1!');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.getByRole('button', { name: 'Use a recovery code instead' }).click();
  await page.getByLabel('Recovery code', { exact: true }).fill('R'.repeat(43));
  await page.getByRole('button', { name: 'Verify and continue' }).click();
  await expect(page.getByRole('alert')).toContainText('Sign-in could not be completed');
  await page.getByRole('button', { name: 'Start again' }).click();
  await expect(page.getByLabel('Password', { exact: true })).toHaveValue('');
});

for (const theme of ['light', 'dark'] as const) {
  test('login accessibility and responsive preview in ' + theme, async ({ page }) => {
    await page.addInitScript(value => { localStorage.setItem('notifyhub-theme', value); }, theme);
    await page.setViewportSize({ width: 1440, height: 1000 });
    await page.goto('/login');
    const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
    expect(results.violations).toEqual([]);
    await page.screenshot({ path: '../document/screenshots/login-' + theme + '.png', fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    if (theme === 'light') await page.screenshot({ path: '../document/screenshots/login-mobile.png', fullPage: true });
  });
}