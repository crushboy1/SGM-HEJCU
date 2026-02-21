import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormularioSalida } from './formulario-salida';

describe('FormularioSalida', () => {
  let component: FormularioSalida;
  let fixture: ComponentFixture<FormularioSalida>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormularioSalida]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormularioSalida);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
