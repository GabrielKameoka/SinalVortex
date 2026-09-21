import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

describe('AuthService registration', () => {
  beforeEach(() => {
    sessionStorage.removeItem('sinalvortex.auth');
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])] });
    spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
  });
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    sessionStorage.removeItem('sinalvortex.auth');
  });
  it('registers and persists only the returned session, then clears it on logout', () => {
    const service = TestBed.inject(AuthService);
    service.register({ nome: 'Gabriel', email: 'g@example.test', senha: 'test-password-123' }).subscribe();
    const request = TestBed.inject(HttpTestingController).expectOne(environment.apiBaseUrl + '/autenticacao/registrar');
    expect(request.request.method).toBe('POST');
    expect(Object.keys(request.request.body).sort()).toEqual(['email', 'nome', 'senha']);
    const response = { usuarioId: 'u', tenantId: 't', accessToken: 'token', expiresAtUtc: '2099-01-01' };
    request.flush(response);
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.tenantId()).toBe('t');
    expect(JSON.parse(sessionStorage.getItem('sinalvortex.auth')!)).toEqual(response);
    service.logout();
    expect(service.tenantId()).toBeNull();
    expect(sessionStorage.getItem('sinalvortex.auth')).toBeNull();
  });
});
