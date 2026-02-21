import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ValidarAdmision } from './validar-admision';

describe('ValidarAdmision', () => {
  let component: ValidarAdmision;
  let fixture: ComponentFixture<ValidarAdmision>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ValidarAdmision]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ValidarAdmision);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
