import { TestBed } from '@angular/core/testing';

import { DocumentoExpediente } from './documento-expediente';

describe('DocumentoExpediente', () => {
  let service: DocumentoExpediente;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DocumentoExpediente);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
