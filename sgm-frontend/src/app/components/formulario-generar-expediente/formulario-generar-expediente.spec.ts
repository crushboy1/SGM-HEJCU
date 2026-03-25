import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormularioGenerarExpediente } from './formulario-generar-expediente';

describe('FormularioGenerarExpediente', () => {
  let component: FormularioGenerarExpediente;
  let fixture: ComponentFixture<FormularioGenerarExpediente>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormularioGenerarExpediente]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormularioGenerarExpediente);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
