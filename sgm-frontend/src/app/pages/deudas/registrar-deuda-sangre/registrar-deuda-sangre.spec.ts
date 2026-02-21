import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegistrarDeudaSangre } from './registrar-deuda-sangre';

describe('RegistrarDeudaSangre', () => {
  let component: RegistrarDeudaSangre;
  let fixture: ComponentFixture<RegistrarDeudaSangre>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegistrarDeudaSangre]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegistrarDeudaSangre);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
