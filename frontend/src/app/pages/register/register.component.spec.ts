import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject } from 'rxjs';
import { AuthResponse, AuthService } from '../../core/auth.service';
import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  const response: AuthResponse = { usuarioId: 'user', tenantId: 'tenant-id', accessToken: 'private-token', expiresAtUtc: '2099-01-01' };
  let pending: Subject<AuthResponse>;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    pending = new Subject<AuthResponse>();
    auth = jasmine.createSpyObj('AuthService', ['register']);
    auth.register.and.returnValue(pending);
    TestBed.configureTestingModule({ imports: [RegisterComponent], providers: [provideRouter([]), { provide: AuthService, useValue: auth }] });
  });

  function setup() {
    const fixture = TestBed.createComponent(RegisterComponent);
    fixture.detectChanges();
    fixture.componentInstance.form.setValue({ nome: ' Gabriel ', email: ' gabriel@example.test ', senha: ' senha-segura-123 ', confirmacao: ' senha-segura-123 ' });
    return fixture;
  }

  it('validates empty fields, password bounds, mismatch and maximum lengths', () => {
    const component = setup().componentInstance;
    component.form.reset();
    component.submit();
    expect(auth.register).not.toHaveBeenCalled();
    expect(component.form.controls.nome.touched).toBeTrue();
    component.form.setValue({ nome: 'a'.repeat(201), email: 'x'.repeat(320) + '@test.com', senha: 'short', confirmacao: 'different' });
    expect(component.form.controls.nome.hasError('maxlength')).toBeTrue();
    expect(component.form.controls.email.invalid).toBeTrue();
    expect(component.form.controls.senha.hasError('minlength')).toBeTrue();
    expect(component.form.hasError('mismatch')).toBeTrue();
    component.form.controls.senha.setValue('a'.repeat(129));
    expect(component.form.controls.senha.hasError('maxlength')).toBeTrue();
  });

  it('trims only name/email and sends one request without confirmation', () => {
    const component = setup().componentInstance;
    component.submit();
    component.submit();
    expect(auth.register).toHaveBeenCalledOnceWith({ nome: 'Gabriel', email: 'gabriel@example.test', senha: ' senha-segura-123 ' });
    expect(component.loading).toBeTrue();
  });

  it('shows tenant, clears passwords and links to dashboard without exposing token', async () => {
    const fixture = setup();
    fixture.componentInstance.submit();
    pending.next(response);
    pending.complete();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('tenant-id');
    expect(fixture.nativeElement.textContent).not.toContain('private-token');
    expect(fixture.nativeElement.querySelector('a[href="/dashboard"]')).toBeTruthy();
    expect(fixture.componentInstance.form.controls.senha.value).toBe('');
    const clipboard = spyOn(navigator.clipboard, 'writeText').and.resolveTo();
    await fixture.componentInstance.copyTenant();
    expect(clipboard).toHaveBeenCalledOnceWith('tenant-id');
    expect(fixture.componentInstance.copyMessage).toContain('copiado');
  });

  it('offers manual copying when clipboard fails', async () => {
    const component = setup().componentInstance;
    component.tenantId = 'tenant-id';
    spyOn(navigator.clipboard, 'writeText').and.rejectWith(new Error('denied'));
    await component.copyTenant();
    expect(component.copyMessage).toContain('Selecione');
  });

  for (const [status, message] of [[0, 'conectar'], [400, 'Confira os dados'], [500, 'mais tarde']] as const) {
    it('handles HTTP ' + status + ' without retry', () => {
      const component = setup().componentInstance;
      component.submit();
      pending.error(new HttpErrorResponse({ status, error: { detail: 'Mensagem de validação' } }));
      expect(component.error).toContain(message);
      expect(component.loading).toBeFalse();
      expect(auth.register).toHaveBeenCalledTimes(1);
      expect(component.tenantId).toBeNull();
    });
  }

  it('links back to login', () => {
    const fixture = setup();
    expect(fixture.nativeElement.querySelector('a[href="/login"]')).toBeTruthy();
  });
});

