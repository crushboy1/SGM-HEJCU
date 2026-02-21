import { TestBed } from '@angular/core/testing';

import { ExpedienteLegal } from './expediente-legal';

describe('ExpedienteLegal', () => {
  let service: ExpedienteLegal;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ExpedienteLegal);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
