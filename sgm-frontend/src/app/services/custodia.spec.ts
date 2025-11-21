import { TestBed } from '@angular/core/testing';

import { Custodia } from './custodia';

describe('Custodia', () => {
  let service: Custodia;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Custodia);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
