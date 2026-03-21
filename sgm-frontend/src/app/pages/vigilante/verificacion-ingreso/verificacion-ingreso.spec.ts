import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VerificacionIngresoComponent } from './verificacion-ingreso';

describe('VerificacionIngreso', () => {
  let component: VerificacionIngresoComponent;
  let fixture: ComponentFixture<VerificacionIngresoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerificacionIngresoComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VerificacionIngresoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
