import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

interface DemoMessage {
  id: string;
  direction: 'inbound' | 'outbound' | 'system';
  text: string;
  subject?: string;
  time: string;
  status?: string;
}

const STORAGE_KEY = 'sinalvortex.demo.messages';

@Component({
  selector: 'sv-demo',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './demo.component.html',
  styleUrl: './demo.component.scss'
})
export class DemoComponent {
  readonly recipient = 'cliente.demo@example.test';
  readonly messages = signal<DemoMessage[]>(this.readMessages());
  subject = 'Atualização do seu pedido';
  text = 'Olá! Esta é uma notificação simulada pelo SinalVortex.';
  sending = false;

  send(): void {
    if (this.sending || !this.subject.trim() || !this.text.trim()) return;
    this.sending = true;
    const now = new Date();
    const outbound: DemoMessage = {
      id: crypto.randomUUID(), direction: 'outbound', subject: this.subject.trim(),
      text: this.text.trim(), time: now.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }), status: 'Enviando'
    };
    const next = [...this.messages(), outbound];
    this.messages.set(next);
    this.persist(next);
    window.setTimeout(() => {
      const delivered = this.messages().map(message => message.id === outbound.id ? { ...message, status: 'Enviado ao sandbox' } : message);
      this.messages.set(delivered);
      this.persist(delivered);
      this.sending = false;
    }, 700);
  }

  clearDemo(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.messages.set(this.seedMessages());
  }

  private seedMessages(): DemoMessage[] {
    return [{
      id: 'welcome', direction: 'inbound', subject: 'Bem-vindo ao SinalVortex',
      text: 'Acompanhe uma conversa de notificações em um só lugar. Escreva uma mensagem ao lado para testar o fluxo.',
      time: '09:41'
    }];
  }

  private readMessages(): DemoMessage[] {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored ? JSON.parse(stored) as DemoMessage[] : this.seedMessages();
    } catch {
      return this.seedMessages();
    }
  }

  private persist(messages: DemoMessage[]): void { localStorage.setItem(STORAGE_KEY, JSON.stringify(messages)); }
}
