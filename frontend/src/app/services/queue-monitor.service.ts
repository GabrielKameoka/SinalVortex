import { Injectable, inject, signal } from '@angular/core';
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
  private connection: HubConnection | null = null;
  readonly queues = signal<QueueSnapshot>(emptySnapshot);
  readonly logs = signal<QueueLog[]>([]);
  readonly connected = signal(false);

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected || !this.auth.token()) return;
    this.connection ??= this.createConnection();
    try {
      await this.connection.start();
      this.connected.set(true);
      this.queues.set(await this.connection.invoke<QueueSnapshot>('GetSnapshot'));
    } catch {
      this.connected.set(false);
    }
  }

  async disconnect(): Promise<void> {
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
      this.queues.set(update.queues);
      this.logs.update((logs) => [update, ...logs].slice(0, 50));
    });
    connection.onreconnecting(() => this.connected.set(false));
    connection.onreconnected(async () => {
      this.connected.set(true);
      this.queues.set(await connection.invoke<QueueSnapshot>('GetSnapshot'));
    });
    connection.onclose(() => this.connected.set(false));
    return connection;
  }
}
