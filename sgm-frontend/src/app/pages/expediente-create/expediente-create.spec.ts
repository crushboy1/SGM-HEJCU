import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExpedienteCreateComponent } from './expediente-create';

describe('ExpedienteCreate', () => {
  let component: ExpedienteCreateComponent;
  let fixture: ComponentFixture<ExpedienteCreateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpedienteCreateComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExpedienteCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
