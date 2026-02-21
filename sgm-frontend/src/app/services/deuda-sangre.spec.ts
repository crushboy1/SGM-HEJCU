import { TestBed } from '@angular/core/testing';

import { DeudaSangre } from './deuda-sangre';

describe('DeudaSangre', () => {
  let service: DeudaSangre;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DeudaSangre);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
