import { TestBed } from '@angular/core/testing';
import { IntegracionService } from './integracion';

describe('IntegracionService', () => {
  let service: IntegracionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(IntegracionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
