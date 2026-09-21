import { environment } from './environment.prod';

describe('Production API configuration', () => {
  it('uses the public HTTPS endpoint without an internal port', () => {
    const url = new URL(environment.apiBaseUrl);
    expect(url.protocol).toBe('https:');
    expect(url.hostname).toBe('sinalvortex-production.up.railway.app');
    expect(url.port).toBe('');
    expect(url.pathname).toBe('/api/v1');
  });
});
