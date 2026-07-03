import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SeniorPanel } from './senior-panel';

describe('SeniorPanel', () => {
  let component: SeniorPanel;
  let fixture: ComponentFixture<SeniorPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SeniorPanel],
    }).compileComponents();

    fixture = TestBed.createComponent(SeniorPanel);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
