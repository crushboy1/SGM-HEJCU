import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BandejaUniversalComponent } from './bandeja-universal';

describe('BandejaUniversal', () => {
  let component: BandejaUniversalComponent;
  let fixture: ComponentFixture<BandejaUniversalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandejaUniversalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BandejaUniversalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
