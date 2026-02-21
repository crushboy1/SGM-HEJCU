import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AutorizarJefeGuardia } from './autorizar-jefe-guardia';

describe('AutorizarJefeGuardia', () => {
  let component: AutorizarJefeGuardia;
  let fixture: ComponentFixture<AutorizarJefeGuardia>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AutorizarJefeGuardia]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AutorizarJefeGuardia);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
