import { TestBed } from '@angular/core/testing';

import { ActaRetiroService } from './acta-retiro';

describe('ActaRetiro', () => {
  let service: ActaRetiroService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ActaRetiroService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
