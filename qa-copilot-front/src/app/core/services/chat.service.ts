import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChatRequest, ChatResponse, ChatMessage } from '../models/chat.model';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly apiUrl = `${environment.apiUrl}/api/chat`;

  constructor(private http: HttpClient) {}

  sendMessage(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<any>(this.apiUrl + '/send', request).pipe(
      map(r => r.data || r)
    );
  }

  getHistory(sessionId: string): Observable<ChatMessage[]> {
    return this.http.get<any>(`${this.apiUrl}/sessions/${sessionId}/history`).pipe(
      map(r => r.data || r)
    );
  }

  getSessions(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl + '/sessions').pipe(
      map(r => r.data || r)
    );
  }
}