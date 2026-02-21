import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SemaforoDeudas } from './semaforo-deudas';

describe('SemaforoDeudas', () => {
  let component: SemaforoDeudas;
  let fixture: ComponentFixture<SemaforoDeudas>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SemaforoDeudas]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SemaforoDeudas);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
