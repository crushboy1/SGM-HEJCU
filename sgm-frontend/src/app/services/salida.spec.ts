import { TestBed } from '@angular/core/testing';

import { Salida } from './salida';

describe('Salida', () => {
  let service: Salida;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Salida);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
