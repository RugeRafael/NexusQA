import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class JiraService {
  private readonly apiUrl = `${environment.apiUrl}/api/jira`;

  constructor(private http: HttpClient) {}

  testConnection(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/test`).pipe(map(r => r.data || r));
  }

  getIssues(maxResults = 20): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/issues?maxResults=${maxResults}`)
      .pipe(map(r => r.data || r));
  }

  getProjects(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/projects`)
      .pipe(map(r => r.data || r));
  }

  getBugsByProject(projectKey: string, assignee?: string): Observable<any[]> {
    let url = `${this.apiUrl}/bugs?projectKey=${projectKey}`;
    if (assignee) url += `&assignee=${assignee}`;
    return this.http.get<any>(url).pipe(map(r => r.data || r));
  }

  getBugsByUrl(issueUrl: string): Observable<any> {
    return this.http.get<any>(
      `${this.apiUrl}/bugs-by-url?url=${encodeURIComponent(issueUrl)}`
    ).pipe(map(r => r.data || r));
  }

  uploadToProject(jiraUrl: string, summary: string, description: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/upload-to-project`, {
      jiraUrl, summary, description
    }).pipe(map(r => r.data || r));
  }

  createTestCase(summary: string, description: string, priority = 'Medium'): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/testcase`, {
      summary, description, priority
    }).pipe(map(r => r.data || r));
  }

  createBug(summary: string, description: string, stepsToReproduce: string, priority = 'High'): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/bug`, {
      summary, description, stepsToReproduce, priority
    }).pipe(map(r => r.data || r));
  }

  extractProjectKeyFromUrl(url: string): string {
    if (!url || !url.trim()) return '';

    const patterns = [
      // https://xxx.atlassian.net/jira/software/c/projects/SEQ/boards
      /\/projects\/([A-Z][A-Z0-9]+)/i,
      // https://xxx.atlassian.net/jira/software/projects/SEQ
      /\/project\/([A-Z][A-Z0-9]+)/i,
      // ?project=SEQ
      /[?&]project=([A-Z][A-Z0-9]+)/i,
      // ?projectKey=SEQ
      /[?&]projectKey=([A-Z][A-Z0-9]+)/i,
      // /browse/SEQ-123
      /\/browse\/([A-Z][A-Z0-9]+)-\d+/i,
    ];

    for (const pattern of patterns) {
      const match = url.match(pattern);
      if (match && match[1]) {
        return match[1].toUpperCase();
      }
    }
    return '';
  }

  extractIssueKeyFromUrl(url: string): string {
    if (!url) return '';
    const match = url.match(/\/browse\/([A-Z][A-Z0-9]+-\d+)/i);
    return match ? match[1].toUpperCase() : '';
  }
}
