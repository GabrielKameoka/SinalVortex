import { Injectable, NgZone, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { AuthService } from '../core/auth.service';
import { environment } from '../../environments/environment';

export interface QueueSnapshot { high: number; normal: number; low: number; dlq: number; }
export interface QueueLog { level: 'info' | 'success' | 'warning' | 'error'; message: string; occurredAtUtc: string; }
interface QueueUpdate extends QueueLog { tenantId: string; queues: QueueSnapshot; }

const emptySnapshot: QueueSnapshot = { high: 0, normal: 0, low: 0, dlq: 0 };

@Injectable({ providedIn: 'root' })
export class QueueMonitorService {
  private readonly auth = inject(AuthService);
  private readonly zone = inject(NgZone);
  private connection: HubConnection | null = null;
  private refreshTimer: ReturnType<typeof setInterval> | null = null;
  private busy = false;
  private stopped = true;
  readonly queues = signal<QueueSnapshot>(emptySnapshot);
  readonly loaded = signal(false);
  readonly error = signal('');
  readonly logs = signal<QueueLog[]>([]);
  readonly connected = signal(false);

  async connect(): Promise<void> {
    this.stopped = false;
    this.refreshTimer ??= setInterval(() => void this.refresh(), 3000);
    await this.refresh();
  }

  private async refresh(): Promise<void> {
    if (this.busy || this.stopped) return;
    if (!this.auth.isAuthenticated()) {
      this.error.set('Sessão expirada. Entre novamente para acompanhar as filas.');
      await this.disconnect();
      return;
    }
    this.busy = true;
    this.connection ??= this.createConnection();
    try {
      if (this.connection.state === HubConnectionState.Disconnected) await this.connection.start();
      if (this.stopped || this.connection.state !== HubConnectionState.Connected) return;
      const snapshot = await this.connection.invoke<QueueSnapshot>('GetSnapshot');
      if (this.stopped) return;
      this.zone.run(() => {
        this.queues.set(snapshot);
        this.loaded.set(true);
        this.connected.set(true);
        this.error.set('');
      });
    } catch {
      this.connected.set(false);
      this.error.set('Sem atualização das filas. Tentando conectar novamente…');
    } finally {
      this.busy = false;
    }
  }

  async disconnect(): Promise<void> {
    this.stopped = true;
    if (this.refreshTimer) clearInterval(this.refreshTimer);
    this.refreshTimer = null;
    if (this.connection) await this.connection.stop();
    this.connected.set(false);
  }

  private createConnection(): HubConnection {
    const hubUrl = `${environment.apiBaseUrl.replace(/\/api\/v1$/, '')}/hubs/queue-monitor`;
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.auth.token() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('queueUpdate', (update: QueueUpdate) => {
      if (this.stopped) return;
      this.zone.run(() => {
        this.queues.set(update.queues);
        this.loaded.set(true);
        this.logs.update((logs) => [update, ...logs].slice(0, 50));
      });
    });
    connection.onreconnecting(() => this.connected.set(false));
    connection.onreconnected(() => void this.refresh());
    connection.onclose(() => this.connected.set(false));
    return connection;
  }
}
