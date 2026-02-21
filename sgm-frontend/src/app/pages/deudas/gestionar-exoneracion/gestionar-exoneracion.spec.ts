import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GestionarExoneracion } from './gestionar-exoneracion';

describe('GestionarExoneracion', () => {
  let component: GestionarExoneracion;
  let fixture: ComponentFixture<GestionarExoneracion>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GestionarExoneracion]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GestionarExoneracion);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
