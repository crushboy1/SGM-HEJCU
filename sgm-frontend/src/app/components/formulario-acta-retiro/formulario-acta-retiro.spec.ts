import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormularioActaRetiro } from './formulario-acta-retiro';

describe('FormularioActaRetiro', () => {
  let component: FormularioActaRetiro;
  let fixture: ComponentFixture<FormularioActaRetiro>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormularioActaRetiro]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormularioActaRetiro);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
