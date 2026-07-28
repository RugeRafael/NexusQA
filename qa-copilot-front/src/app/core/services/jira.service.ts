import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class JiraService {
  private readonly apiUrl = environment.apiUrl + '/api/jira';

  constructor(private http: HttpClient) {}

  testConnection(): Observable<any> {
    return this.http.get<any>(this.apiUrl + '/test-connection').pipe(map(r => r.data || r));
  }

  getIssues(maxResults = 20): Observable<any[]> {
    return this.http.get<any>(this.apiUrl + '/issues?maxResults=' + maxResults)
      .pipe(map(r => r.data || r));
  }

  getProjects(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl + '/projects').pipe(map(r => r.data || r));
  }

  getBugsByProject(projectKey: string, assignee?: string): Observable<any[]> {
    let url = this.apiUrl + '/bugs?projectKey=' + projectKey;
    if (assignee) url += '&assignee=' + assignee;
    return this.http.get<any>(url).pipe(map(r => r.data || r));
  }

  getBugsByUrl(issueUrl: string): Observable<any> {
    return this.http.get<any>(
      this.apiUrl + '/bugs-by-url?url=' + encodeURIComponent(issueUrl)
    ).pipe(map(r => r.data || r));
  }

  uploadToProject(jiraUrl: string, summary: string, description: string): Observable<any> {
    return this.createIssueWithHtml(summary, description, 'Task', 'Medium', '', '', jiraUrl);
  }

  createTestCase(summary: string, description: string, priority = 'Medium'): Observable<any> {
    return this.http.post<any>(this.apiUrl + '/issues', {
      summary, description, issueType: 'Task', priority
    }).pipe(map(r => r.data || r));
  }

  createBug(summary: string, description: string, stepsToReproduce: string, priority = 'High'): Observable<any> {
    return this.http.post<any>(this.apiUrl + '/issues', {
      summary,
      description: description + '\n\nPasos para reproducir:\n' + stepsToReproduce,
      issueType: 'Bug', priority
    }).pipe(map(r => r.data || r));
  }

  createIssueWithHtml(
    summary: string,
    description: string,
    issueType: string,
    priority: string,
    htmlContent: string,
    fileName: string,
    jiraUrl?: string
  ): Observable<any> {
    const formData = new FormData();
    formData.append('summary', summary);
    formData.append('description', description);
    formData.append('issueType', issueType);
    formData.append('priority', priority);
    if (jiraUrl) formData.append('jiraUrl', jiraUrl);
    if (htmlContent) {
      const blob = new Blob([htmlContent], { type: 'text/html' });
      formData.append('file', blob, fileName || 'informe.html');
    }
    return this.http.post<any>(this.apiUrl + '/issues/create-with-attachment', formData)
      .pipe(map(r => r.data || r));
  }

  attachHtmlToIssue(issueKey: string, htmlContent: string, fileName: string): Observable<any> {
    const formData = new FormData();
    const blob = new Blob([htmlContent], { type: 'text/html' });
    formData.append('file', blob, fileName);
    return this.http.post<any>(this.apiUrl + '/issues/' + issueKey + '/attach', formData)
      .pipe(map(r => r.data || r));
  }

  extractProjectKeyFromUrl(url: string): string {
    if (!url || !url.trim()) return '';
    const patterns = [
      /\/projects\/([A-Z][A-Z0-9]+)/i,
      /\/project\/([A-Z][A-Z0-9]+)/i,
      /[?&]project=([A-Z][A-Z0-9]+)/i,
      /[?&]projectKey=([A-Z][A-Z0-9]+)/i,
      /\/browse\/([A-Z][A-Z0-9]+)-\d+/i,
    ];
    for (const pattern of patterns) {
      const match = url.match(pattern);
      if (match && match[1]) return match[1].toUpperCase();
    }
    return '';
  }

  extractIssueKeyFromUrl(url: string): string {
    if (!url) return '';
    const match = url.match(/\/browse\/([A-Z][A-Z0-9]+-\d+)/i);
    return match ? match[1].toUpperCase() : '';
  }
}
