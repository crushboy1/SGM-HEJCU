import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConsultaExpedientes } from './consulta-expedientes';

describe('ConsultaExpedientes', () => {
  let component: ConsultaExpedientes;
  let fixture: ComponentFixture<ConsultaExpedientes>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConsultaExpedientes]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ConsultaExpedientes);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
