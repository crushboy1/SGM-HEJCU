import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BusquedaSalida } from './busqueda-salida';

describe('BusquedaSalida', () => {
  let component: BusquedaSalida;
  let fixture: ComponentFixture<BusquedaSalida>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BusquedaSalida]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BusquedaSalida);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
