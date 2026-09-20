import { TestBed } from '@angular/core/testing';
import { QueueCountComponent } from './queue-count.component';

describe('QueueCountComponent', () => {
  it('distinguishes unavailable data from zero and displays the latest real count', () => {
    const fixture = TestBed.createComponent(QueueCountComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.trim()).toBe('—');
    fixture.componentRef.setInput('value', 0);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.trim()).toBe('0');
    fixture.componentRef.setInput('value', 5);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent.trim()).toBe('5');
    fixture.destroy();
  });
});
