import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BandejaEntradaComponent } from './bandeja-entrada';

describe('BandejaEntradaComponent', () => {
  let component: BandejaEntradaComponent;
  let fixture: ComponentFixture<BandejaEntradaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BandejaEntradaComponent]
    })
      .compileComponents();

    fixture = TestBed.createComponent(BandejaEntradaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
