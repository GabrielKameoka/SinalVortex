import { Injectable, signal } from '@angular/core';

type Theme = 'light' | 'dark';
const KEY = 'sinalvortex.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<Theme>(this.initialTheme());
  constructor() { this.apply(this.theme()); }
  toggle(): void { const next = this.theme() === 'dark' ? 'light' : 'dark'; this.theme.set(next); localStorage.setItem(KEY, next); this.apply(next); }
  private initialTheme(): Theme { const saved = localStorage.getItem(KEY); if (saved === 'light' || saved === 'dark') return saved; return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'; }
  private apply(theme: Theme): void { document.documentElement.dataset['theme'] = theme; }
}
