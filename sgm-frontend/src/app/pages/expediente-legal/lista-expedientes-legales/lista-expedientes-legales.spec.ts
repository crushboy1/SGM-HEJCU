import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListaExpedientesLegales } from './lista-expedientes-legales';

describe('ListaExpedientesLegales', () => {
  let component: ListaExpedientesLegales;
  let fixture: ComponentFixture<ListaExpedientesLegales>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListaExpedientesLegales]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListaExpedientesLegales);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
