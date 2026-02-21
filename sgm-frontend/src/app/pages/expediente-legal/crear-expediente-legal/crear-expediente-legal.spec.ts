import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CrearExpedienteLegal } from './crear-expediente-legal';

describe('CrearExpedienteLegal', () => {
  let component: CrearExpedienteLegal;
  let fixture: ComponentFixture<CrearExpedienteLegal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CrearExpedienteLegal]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CrearExpedienteLegal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
