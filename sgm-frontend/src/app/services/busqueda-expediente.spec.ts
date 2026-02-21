import { TestBed } from '@angular/core/testing';

import { BusquedaExpediente } from './busqueda-expediente';

describe('BusquedaExpediente', () => {
  let service: BusquedaExpediente;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BusquedaExpediente);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
