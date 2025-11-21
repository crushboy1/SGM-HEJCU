import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MisTareas } from './mis-tareas';

describe('MisTareas', () => {
  let component: MisTareas;
  let fixture: ComponentFixture<MisTareas>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MisTareas]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MisTareas);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
