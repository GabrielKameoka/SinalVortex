import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeService } from '../../core/theme.service';
import { CrmContact, InboxChannel, TimelineEvent, inboxConversations } from './inbox.mock';
@Component({selector:'sv-inbox',standalone:true,imports:[CommonModule,DatePipe,RouterLink],templateUrl:'./inbox.component.html',styleUrl:'./inbox.component.scss'})
export class InboxComponent {
  readonly conversations=inboxConversations; readonly selectedId=signal(inboxConversations[0].id);
  readonly selectedConversation=computed(()=>this.conversations.find(c=>c.id===this.selectedId()) ?? this.conversations[0]);
  readonly timeline=computed(()=>[...this.selectedConversation().timeline].sort((a,b)=>new Date(a.occurredAt).getTime()-new Date(b.occurredAt).getTime()));
  constructor(readonly theme:ThemeService) {}
  selectConversation(id:string):void { this.selectedId.set(id); }
  initials(contact:CrmContact):string { return contact.name.split(' ').slice(0,2).map(p=>p[0]).join(''); }
  channelLabel(channel:InboxChannel):string { return {email:'E-mail',whatsapp:'WhatsApp',sms:'SMS',webhook:'Webhook',push:'Push'}[channel]; }
  day(event:TimelineEvent):string { return new Date(event.occurredAt).toLocaleDateString('pt-BR',{day:'2-digit',month:'long'}); }
}
