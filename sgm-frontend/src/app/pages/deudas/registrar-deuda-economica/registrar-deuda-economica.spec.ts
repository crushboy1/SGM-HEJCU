import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegistrarDeudaEconomica } from './registrar-deuda-economica';

describe('RegistrarDeudaEconomica', () => {
  let component: RegistrarDeudaEconomica;
  let fixture: ComponentFixture<RegistrarDeudaEconomica>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegistrarDeudaEconomica]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegistrarDeudaEconomica);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
