import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { InboxComponent } from './inbox.component';
import { NotificationsService } from '../../services/notifications.service';
import { ThemeService } from '../../core/theme.service';

describe('InboxComponent real data', () => {
  function setup(list: jasmine.Spy) {
    TestBed.configureTestingModule({ providers: [
      { provide: NotificationsService, useValue: { list } },
      { provide: ThemeService, useValue: {} }
    ] });
    return TestBed.runInInjectionContext(() => new InboxComponent());
  }

  it('keeps an empty API response empty instead of showing sample conversations', fakeAsync(() => {
    const list = jasmine.createSpy().and.returnValue(of({ items: [], totalCount: 0, totalPages: 0, pageNumber: 1 }));
    const component = setup(list);
    tick(0);
    expect(component.page()?.totalCount).toBe(0);
    expect(component.selected()).toBeNull();
    tick(5000);
    expect(list).toHaveBeenCalledTimes(2);
    TestBed.resetTestingModule();
  }));

  it('reports API failures instead of fabricating notifications', fakeAsync(() => {
    const component = setup(jasmine.createSpy().and.returnValue(throwError(() => new Error('offline'))));
    tick(0);
    expect(component.error()).toContain('Não foi possível');
    expect(component.selected()).toBeNull();
    TestBed.resetTestingModule();
  }));
});
