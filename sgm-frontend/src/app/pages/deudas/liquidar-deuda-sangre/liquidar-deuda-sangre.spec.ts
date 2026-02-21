import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LiquidarDeudaSangre } from './liquidar-deuda-sangre';

describe('LiquidarDeudaSangre', () => {
  let component: LiquidarDeudaSangre;
  let fixture: ComponentFixture<LiquidarDeudaSangre>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LiquidarDeudaSangre]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LiquidarDeudaSangre);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
