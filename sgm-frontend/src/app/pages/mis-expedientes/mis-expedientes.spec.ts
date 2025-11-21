import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MisExpedientes } from './mis-expedientes';

describe('MisExpedientes', () => {
  let component: MisExpedientes;
  let fixture: ComponentFixture<MisExpedientes>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MisExpedientes]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MisExpedientes);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
