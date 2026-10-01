import { test, expect } from '../../Frontend/node_modules/@playwright/test';
import AxeBuilder from '../../Frontend/node_modules/@axe-core/playwright';

for (const theme of ['light', 'dark'] as const) {
  test('automated accessibility baseline in ' + theme, async ({ page }) => {
    await page.route('**/health', route => route.fulfill({ contentType: 'application/json', body: '{"status":"healthy"}' }));
    await page.emulateMedia({ colorScheme: theme, reducedMotion: 'reduce' });
    await page.goto('/');
    await expect(page.getByRole('status')).toHaveText('API is reachable');
    const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
    expect(results.violations).toEqual([]);
  });
}
