import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, EMPTY, catchError, switchMap, timer } from 'rxjs';
import { ThemeService } from '../../core/theme.service';
import { AuthService } from '../../core/auth.service';
import { NotificationPage, NotificationsService } from '../../services/notifications.service';

interface NotificationConversation {
  key: string;
  recipient: string;
  items: NotificationPage['items'];
  latest: NotificationPage['items'][number];
}

@Component({ selector: 'sv-inbox', standalone: true, imports: [CommonModule, RouterLink], templateUrl: './inbox.component.html', styleUrl: './inbox.component.scss' })
export class InboxComponent {
  readonly theme = inject(ThemeService);
  readonly auth = inject(AuthService);
  private readonly service = inject(NotificationsService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly requestedPage = new BehaviorSubject(1);
  readonly page = signal<NotificationPage | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly selectedId = signal<string | null>(null);
  readonly conversations = computed<NotificationConversation[]>(() => {
    const groups = new Map<string, NotificationConversation>();
    for (const item of this.page()?.items ?? []) {
      const key = item.destinatario.trim().toLowerCase();
      const conversation = groups.get(key);
      if (conversation) {
        conversation.items.push(item);
        if (new Date(item.criadoEm) > new Date(conversation.latest.criadoEm)) conversation.latest = item;
      } else {
        groups.set(key, { key, recipient: item.destinatario, items: [item], latest: item });
      }
    }
    return [...groups.values()]
      .map(conversation => ({
        ...conversation,
        items: [...conversation.items].sort((left, right) =>
          new Date(left.criadoEm).getTime() - new Date(right.criadoEm).getTime())
      }))
      .sort((left, right) =>
        new Date(right.latest.criadoEm).getTime() - new Date(left.latest.criadoEm).getTime());
  });
  readonly selectedConversationKey = computed(() => this.selectedId() ?? (this.conversations().length > 0 ? this.conversations()[0].key : null));
  readonly selectedConversation = computed(() => this.conversations().find(item => item.key === this.selectedId()) ?? this.conversations()[0] ?? null);
  readonly selected = computed(() => this.selectedConversation()?.latest ?? null);

  constructor() {
    this.requestedPage.pipe(
      switchMap(page => timer(0, 5000).pipe(switchMap(() => this.service.list(page).pipe(
        catchError(() => {
          this.loading.set(false);
          this.error.set('Não foi possível atualizar as notificações. Os últimos dados recebidos podem estar desatualizados.');
          return EMPTY;
        })
      )))),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => { this.page.set(result); this.loading.set(false); this.error.set(''); });
  }

  goTo(page: number): void {
    if (page < 1 || page > (this.page()?.totalPages ?? 1)) return;
    this.loading.set(true);
    this.selectedId.set(null);
    this.requestedPage.next(page);
  }
  channelLabel(channel: number): string { return ({ 1: 'E-mail', 2: 'WhatsApp', 3: 'SMS', 4: 'Webhook', 5: 'Push' } as Record<number, string>)[channel] ?? 'Desconhecido'; }
  statusLabel(status: number): string { return ({ 1: 'Pendente', 2: 'Em processamento', 3: 'Enviado ao provedor', 4: 'Falhou', 5: 'DLQ' } as Record<number, string>)[status] ?? 'Desconhecido'; }
  logout(): void { this.auth.logout(); }
}
