import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VerificacionIngreso } from './verificacion-ingreso';

describe('VerificacionIngreso', () => {
  let component: VerificacionIngreso;
  let fixture: ComponentFixture<VerificacionIngreso>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerificacionIngreso]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VerificacionIngreso);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
