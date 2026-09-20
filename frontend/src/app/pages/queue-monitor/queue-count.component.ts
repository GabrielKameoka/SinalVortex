import { DecimalPipe } from '@angular/common';
import { Component, ElementRef, Input, OnChanges, SimpleChanges, inject } from '@angular/core';

@Component({
  selector: 'sv-queue-count',
  standalone: true,
  imports: [DecimalPipe],
  template: `{{ value === null ? '—' : (value | number) }}`,
  styles: [':host { display: inline-block; font-variant-numeric: tabular-nums; }']
})
export class QueueCountComponent implements OnChanges {
  @Input() value: number | null = null;
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);

  ngOnChanges(changes: SimpleChanges): void {
    const change = changes['value'];
    if (!change || change.firstChange || change.previousValue === this.value ||
        window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    const element = this.element.nativeElement;
    element.getAnimations().forEach(animation => animation.cancel());
    element.animate([
      { transform: 'translateY(5px)', opacity: 0.4 },
      { transform: 'translateY(0)', opacity: 1 }
    ], { duration: 240, easing: 'ease-out' });
  }
}
