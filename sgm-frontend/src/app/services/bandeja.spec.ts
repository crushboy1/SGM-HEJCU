import { TestBed } from '@angular/core/testing';
import { BandejaService } from './bandeja';

describe('BandejaService', () => {
  let service: BandejaService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BandejaService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
