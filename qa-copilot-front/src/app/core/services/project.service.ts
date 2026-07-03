import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Project, CreateProjectRequest, AssignProjectRequest } from '../models/project.model';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly apiUrl = `${environment.apiUrl}/api/projects`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Project[]> {
    return this.http.get<any>(this.apiUrl).pipe(map(r => r.data || r));
  }

  getMyProjects(): Observable<Project[]> {
    return this.http.get<any>(this.apiUrl + '/my-projects').pipe(map(r => r.data || r));
  }

  create(request: CreateProjectRequest): Observable<Project> {
    return this.http.post<any>(this.apiUrl, request).pipe(map(r => r.data || r));
  }

  assign(request: AssignProjectRequest): Observable<Project> {
    return this.http.post<any>(this.apiUrl + '/assign', request).pipe(map(r => r.data || r));
  }

  unassign(projectId: string, userId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${projectId}/users/${userId}`);
  }
}