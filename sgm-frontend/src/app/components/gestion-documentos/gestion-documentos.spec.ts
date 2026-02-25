import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GestionDocumentos } from './gestion-documentos';

describe('GestionDocumentos', () => {
  let component: GestionDocumentos;
  let fixture: ComponentFixture<GestionDocumentos>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GestionDocumentos]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GestionDocumentos);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
