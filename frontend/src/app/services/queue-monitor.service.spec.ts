import { TestBed, fakeAsync, flushMicrotasks, tick } from '@angular/core/testing';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AuthService } from '../core/auth.service';
import { QueueMonitorService } from './queue-monitor.service';

describe('QueueMonitorService', () => {
  function setup(failFirst = false) {
    let attempts = 0;
    const handlers: Record<string, (...args: any[]) => void> = {};
    const connection = {
      state: HubConnectionState.Disconnected,
      start: jasmine.createSpy().and.callFake(async () => {
        if (failFirst && attempts++ === 0) throw new Error('API unavailable');
        connection.state = HubConnectionState.Connected;
      }),
      stop: jasmine.createSpy().and.callFake(async () => { connection.state = HubConnectionState.Disconnected; }),
      invoke: jasmine.createSpy().and.resolveTo({ high: 2, normal: 0, low: 1, dlq: 5 }),
      on: (event: string, handler: (...args: any[]) => void) => { handlers[event] = handler; },
      onreconnecting: () => {}, onreconnected: () => {}, onclose: () => {}
    };
    spyOn(HubConnectionBuilder.prototype, 'build').and.returnValue(connection as unknown as HubConnection);
    TestBed.configureTestingModule({ providers: [{ provide: AuthService, useValue: { token: () => 'test-token', isAuthenticated: () => true } }] });
    return { service: TestBed.inject(QueueMonitorService), connection, handlers };
  }

  it('retries a failed first connection and loads the real snapshot', fakeAsync(() => {
    const { service, connection } = setup(true);
    void service.connect(); flushMicrotasks();
    expect(service.connected()).toBeFalse();
    expect(service.loaded()).toBeFalse();
    tick(3000); flushMicrotasks();
    expect(connection.start).toHaveBeenCalledTimes(2);
    expect(service.queues().dlq).toBe(5);
    expect(service.connected()).toBeTrue();
    void service.disconnect(); flushMicrotasks();
  }));

  it('uses received events and stops refreshing after leaving the screen', fakeAsync(() => {
    const { service, connection, handlers } = setup();
    void service.connect(); flushMicrotasks();
    handlers['queueUpdate']({ tenantId: 'test', queues: { high: 0, normal: 1, low: 0, dlq: 0 }, level: 'info', message: 'Enfileirada', occurredAtUtc: '2026-09-18T12:00:00Z' });
    expect(service.logs().map(log => log.message)).toEqual(['Enfileirada']);
    expect(service.queues().normal).toBe(1);
    void service.disconnect(); flushMicrotasks();
    const calls = connection.invoke.calls.count();
    tick(6000);
    expect(connection.invoke.calls.count()).toBe(calls);
  }));
});
