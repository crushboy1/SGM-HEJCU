import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormularioActaRetiroComponent } from './formulario-acta-retiro';

describe('FormularioActaRetiro', () => {
  let component: FormularioActaRetiroComponent;
  let fixture: ComponentFixture<FormularioActaRetiroComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormularioActaRetiroComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormularioActaRetiroComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
