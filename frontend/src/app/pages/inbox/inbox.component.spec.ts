import { InboxComponent } from './inbox.component';
import { ThemeService } from '../../core/theme.service';

describe('InboxComponent', () => {
  const component = () => new InboxComponent({} as ThemeService);

  it('selects a conversation and exposes its CRM profile', () => {
    const subject = component();
    subject.selectConversation('carlos');

    expect(subject.selectedConversation().contact.name).toBe('Carlos Eduardo');
    expect(subject.selectedConversation().contact.tags).toContain('ia: financeiro');
  });

  it('orders unified timeline events chronologically', () => {
    const subject = component();
    const events = subject.timeline();

    expect(events.map(event => event.id)).toEqual(['a1', 'a2', 'a3', 'a4']);
  });

  it('provides accessible labels for every supported channel', () => {
    const subject = component();
    expect(subject.channelLabel('whatsapp')).toBe('WhatsApp');
    expect(subject.channelLabel('webhook')).toBe('Webhook');
  });
});
