import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Document } from '../models/document.model';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly apiUrl = `${environment.apiUrl}/api/documents`;

  constructor(private http: HttpClient) {}

  upload(file: File, description?: string): Observable<Document> {
    const formData = new FormData();
    formData.append('file', file);
    if (description) formData.append('description', description);
    return this.http.post<any>(this.apiUrl + '/upload', formData).pipe(
      map(r => r.data || r)
    );
  }

  getMyDocuments(): Observable<Document[]> {
    return this.http.get<any>(this.apiUrl).pipe(
      map(r => r.data || r)
    );
  }
}