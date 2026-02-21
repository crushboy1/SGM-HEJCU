import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DetalleExpedienteLegal } from './detalle-expediente-legal';

describe('DetalleExpedienteLegal', () => {
  let component: DetalleExpedienteLegal;
  let fixture: ComponentFixture<DetalleExpedienteLegal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DetalleExpedienteLegal]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DetalleExpedienteLegal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
