import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapaMortuorio } from './mapa-mortuorio';

describe('MapaMortuorio', () => {
  let component: MapaMortuorio;
  let fixture: ComponentFixture<MapaMortuorio>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MapaMortuorio]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapaMortuorio);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
