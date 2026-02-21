import { TestBed } from '@angular/core/testing';

import { DeudaEconomica } from './deuda-economica';

describe('DeudaEconomica', () => {
  let service: DeudaEconomica;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DeudaEconomica);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
