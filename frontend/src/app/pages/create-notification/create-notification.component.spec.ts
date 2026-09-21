import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CreateNotificationComponent } from './create-notification.component';
import { environment } from '../../../environments/environment';

describe('CreateNotificationComponent with NotificationsService', () => {
  beforeEach(() => TestBed.configureTestingModule({
    imports: [CreateNotificationComponent],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
  }));
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function setup() {
    const fixture = TestBed.createComponent(CreateNotificationComponent);
    fixture.detectChanges();
    fixture.componentInstance.form.patchValue({ destinatario: 'demo@example.test', assunto: 'Demonstração', conteudo: 'Mensagem local' });
    return fixture;
  }

  it('posts the real numeric contract once and displays the created ID with tracking links', () => {
    const fixture = setup();
    fixture.componentInstance.submit();
    fixture.componentInstance.submit();
    const request = TestBed.inject(HttpTestingController).expectOne(`${environment.apiBaseUrl}/notificacoes`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ destinatario: 'demo@example.test', assunto: 'Demonstração', conteudo: 'Mensagem local', canal: 1, prioridade: 2, aplicacaoId: jasmine.stringMatching(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i) });
    request.flush({ id: 'created-id', status: 1, criadoEm: '2026-09-21' }, { status: 201, statusText: 'Created' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('created-id');
    expect(fixture.nativeElement.querySelector('a[href="/inbox"]')).toBeTruthy();
    fixture.componentInstance.submit();
    TestBed.inject(HttpTestingController).expectNone(`${environment.apiBaseUrl}/notificacoes`);
  });

  it('requires email subject and rejects blank content without issuing requests', () => {
    const component = setup().componentInstance;
    component.form.patchValue({ assunto: ' ' });
    component.submit();
    expect(component.form.hasError('subjectRequired')).toBeTrue();
    component.form.patchValue({ assunto: 'Ok', conteudo: ' ' });
    component.submit();
    expect(component.form.controls.conteudo.invalid).toBeTrue();
    TestBed.inject(HttpTestingController).expectNone(`${environment.apiBaseUrl}/notificacoes`);
  });

  it('allows SMS without subject and does not retry after an ambiguous network failure', () => {
    const component = setup().componentInstance;
    component.form.patchValue({ canal: 3, destinatario: '5511999999999', assunto: '' });
    component.submit();
    const request = TestBed.inject(HttpTestingController).expectOne(`${environment.apiBaseUrl}/notificacoes`);
    expect(request.request.body.assunto).toBeNull();
    expect(request.request.body.canal).toBe(3);
    request.error(new ProgressEvent('error'));
    expect(component.loading).toBeFalse();
    expect(component.error).toContain('Confira o inbox antes de reenviar');
    TestBed.inject(HttpTestingController).expectNone(`${environment.apiBaseUrl}/notificacoes`);
  });
});
