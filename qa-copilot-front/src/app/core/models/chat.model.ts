export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  sentAt: string;
}

export interface ChatRequest {
  message: string;
  sessionId?: string;
}

export interface ChatResponse {
  sessionId: string;
  response: string;
  history: ChatMessage[];
}