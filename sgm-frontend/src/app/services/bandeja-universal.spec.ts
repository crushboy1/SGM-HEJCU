import { TestBed } from '@angular/core/testing';

import { BandejaUniversal } from './bandeja-universal';

describe('BandejaUniversal', () => {
  let service: BandejaUniversal;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BandejaUniversal);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
