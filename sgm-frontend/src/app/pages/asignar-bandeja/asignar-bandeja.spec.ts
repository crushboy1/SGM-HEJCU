import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AsignarBandeja } from './asignar-bandeja';

describe('AsignarBandeja', () => {
  let component: AsignarBandeja;
  let fixture: ComponentFixture<AsignarBandeja>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AsignarBandeja]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AsignarBandeja);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
