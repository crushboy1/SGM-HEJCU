import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TimelineEstados } from './timeline-estados';

describe('TimelineEstados', () => {
  let component: TimelineEstados;
  let fixture: ComponentFixture<TimelineEstados>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TimelineEstados]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TimelineEstados);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
