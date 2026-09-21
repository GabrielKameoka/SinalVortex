import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface NotificationItem {
  id: string; aplicacaoId: string; destinatario: string; canal: number;
  prioridade: number; status: number; conteudo: string; assunto: string | null;
  tentativas: number; maxTentativas: number; processadoEm: string | null; criadoEm: string;
}
export interface NotificationPage {
  items: NotificationItem[]; pageNumber: number; totalPages: number;
  totalCount: number; hasNextPage: boolean; hasPreviousPage: boolean;
}

export interface CreateNotificationRequest {
  aplicacaoId: string; destinatario: string; canal: number;
  prioridade: number; conteudo: string; assunto: string | null;
}
export interface CreateNotificationResponse { id: string; status: number; criadoEm: string; }

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);
  create(request: CreateNotificationRequest) {
    return this.http.post<CreateNotificationResponse>(`${environment.apiBaseUrl}/notificacoes`, request);
  }
  list(page: number) {
    return this.http.get<NotificationPage>(`${environment.apiBaseUrl}/notificacoes`, {
      params: { pageNumber: page, pageSize: 20 }
    });
  }
}
