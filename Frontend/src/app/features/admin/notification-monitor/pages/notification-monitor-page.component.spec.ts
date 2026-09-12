import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NotificationMonitorPageComponent } from './notification-monitor-page.component';

describe('NotificationMonitorPageComponent', () => {
  let component: NotificationMonitorPageComponent;
  let fixture: ComponentFixture<NotificationMonitorPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotificationMonitorPageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(NotificationMonitorPageComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
