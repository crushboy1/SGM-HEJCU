import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormDeudaSangre } from './form-deuda-sangre';

describe('FormDeudaSangre', () => {
  let component: FormDeudaSangre;
  let fixture: ComponentFixture<FormDeudaSangre>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormDeudaSangre]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FormDeudaSangre);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
