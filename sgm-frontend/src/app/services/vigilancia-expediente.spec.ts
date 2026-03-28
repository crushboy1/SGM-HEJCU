import { TestBed } from '@angular/core/testing';

import { VigilanciaExpediente } from './vigilancia-expediente';

describe('VigilanciaExpediente', () => {
  let service: VigilanciaExpediente;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(VigilanciaExpediente);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
