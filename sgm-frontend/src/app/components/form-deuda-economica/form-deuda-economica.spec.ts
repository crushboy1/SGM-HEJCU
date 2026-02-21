import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormDeudaEconomica } from './form-deuda-economica';

describe('FormDeudaEconomica', () => {
  let component: FormDeudaEconomica;
  let fixture: ComponentFixture<FormDeudaEconomica>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormDeudaEconomica]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormDeudaEconomica);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
