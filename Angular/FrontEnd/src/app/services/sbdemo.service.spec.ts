import { TestBed } from '@angular/core/testing';

import { SbdemoService } from './sbdemo.service';

describe('SbdemoService', () => {
  let service: SbdemoService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SbdemoService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
