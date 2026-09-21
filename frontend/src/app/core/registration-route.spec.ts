import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { routes } from '../app.routes';
import { AuthService } from './auth.service';
import { LoginComponent } from '../pages/login/login.component';

describe('Registration navigation', () => {
  for (const authenticated of [false, true]) {
    it('handles registration access when authenticated=' + authenticated, () => {
      TestBed.configureTestingModule({ providers: [
        provideRouter(routes),
        { provide: AuthService, useValue: { isAuthenticated: () => authenticated } }
      ] });
      const guard = routes.find(route => route.path === 'registro')!.canActivate![0] as () => boolean | UrlTree;
      const result = TestBed.runInInjectionContext(guard);
      if (authenticated) expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/dashboard');
      else expect(result).toBeTrue();
    });
  }

  it('offers a registration link on the login page', () => {
    TestBed.configureTestingModule({ imports: [LoginComponent], providers: [
      provideRouter(routes), { provide: AuthService, useValue: {} }
    ] });
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('a[href="/registro"]')).toBeTruthy();
  });
});
