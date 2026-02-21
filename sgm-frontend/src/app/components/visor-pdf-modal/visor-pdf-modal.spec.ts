import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VisorPdfModal } from './visor-pdf-modal';

describe('VisorPdfModal', () => {
  let component: VisorPdfModal;
  let fixture: ComponentFixture<VisorPdfModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VisorPdfModal]
    })
    .compileComponents();

    fixture = TestBed.createComponent(VisorPdfModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
