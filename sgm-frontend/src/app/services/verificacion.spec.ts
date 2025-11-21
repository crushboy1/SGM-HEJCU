import { TestBed } from '@angular/core/testing';

import { Verificacion } from './verificacion';

describe('Verificacion', () => {
  let service: Verificacion;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Verificacion);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
