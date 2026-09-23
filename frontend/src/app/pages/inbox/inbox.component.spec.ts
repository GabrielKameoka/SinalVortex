import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { InboxComponent } from './inbox.component';
import { NotificationsService } from '../../services/notifications.service';
import { ThemeService } from '../../core/theme.service';
import { AuthService } from '../../core/auth.service';

describe('InboxComponent real data', () => {
  function setup(list: jasmine.Spy) {
    TestBed.configureTestingModule({ providers: [
      { provide: NotificationsService, useValue: { list } },
      { provide: ThemeService, useValue: {} },
      { provide: AuthService, useValue: jasmine.createSpyObj('AuthService', ['logout']) }
    ] });
    return TestBed.runInInjectionContext(() => new InboxComponent());
  }

  it('keeps an empty API response empty instead of showing sample conversations', fakeAsync(() => {
    const list = jasmine.createSpy().and.returnValue(of({ items: [], totalCount: 0, totalPages: 0, pageNumber: 1 }));
    const component = setup(list);
    tick(0);
    expect(component.page()?.totalCount).toBe(0);
    expect(component.selected()).toBeNull();
    tick(5000);
    expect(list).toHaveBeenCalledTimes(2);
    TestBed.resetTestingModule();
  }));

  it('reports API failures instead of fabricating notifications', fakeAsync(() => {
    const component = setup(jasmine.createSpy().and.returnValue(throwError(() => new Error('offline'))));
    tick(0);
    expect(component.error()).toContain('Não foi possível');
    expect(component.selected()).toBeNull();
    TestBed.resetTestingModule();
  }));

  it('groups notifications from the same recipient into one conversation', fakeAsync(() => {
    const items = [
      { id: '3', aplicacaoId: 'app', destinatario: 'outro@example.test', canal: 1, prioridade: 2, status: 3, conteudo: 'Outra', assunto: 'Outro', tentativas: 1, maxTentativas: 3, processadoEm: null, criadoEm: '2026-09-22T12:00:00Z' },
      { id: '2', aplicacaoId: 'app', destinatario: 'cliente@example.test', canal: 1, prioridade: 2, status: 3, conteudo: 'Segundo', assunto: 'Segundo', tentativas: 1, maxTentativas: 3, processadoEm: null, criadoEm: '2026-09-22T11:00:00Z' },
      { id: '1', aplicacaoId: 'app', destinatario: 'CLIENTE@example.test', canal: 1, prioridade: 2, status: 3, conteudo: 'Primeiro', assunto: 'Primeiro', tentativas: 1, maxTentativas: 3, processadoEm: null, criadoEm: '2026-09-22T10:00:00Z' }
    ];
    const component = setup(jasmine.createSpy().and.returnValue(of({ items, totalCount: 3, totalPages: 1, pageNumber: 1 })));
    tick(0);
    expect(component.conversations().length).toBe(2);
    expect(component.conversations().find(conversation => conversation.key === 'cliente@example.test')?.items.length).toBe(2);
    TestBed.resetTestingModule();
  }));
});
