import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LiquidarDeudaEconomica } from './liquidar-deuda-economica';

describe('LiquidarDeudaEconomica', () => {
  let component: LiquidarDeudaEconomica;
  let fixture: ComponentFixture<LiquidarDeudaEconomica>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LiquidarDeudaEconomica]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LiquidarDeudaEconomica);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
