import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GenerateTestCaseRequest, TestCaseResponse } from '../models/document.model';
import { TestCaseHistory } from '../models/testcase.model';

@Injectable({ providedIn: 'root' })
export class TestcaseService {
  private readonly apiUrl = `${environment.apiUrl}/api`;

  constructor(private http: HttpClient) {}

  generate(request: GenerateTestCaseRequest): Observable<TestCaseResponse> {
  return this.http.post<any>(`${this.apiUrl}/testcases/generate`, request).pipe(
    map(r => {
      const data = r.data || r;
      return data;
    })
  );
}

  getHistory(page = 1, pageSize = 10): Observable<any> {
    return this.http.get<any>(
      `${this.apiUrl}/history?page=${page}&pageSize=${pageSize}`
    ).pipe(map(r => r.data || r));
  }
}