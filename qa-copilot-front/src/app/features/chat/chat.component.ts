import { Component, OnInit, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ChatService } from '../../core/services/chat.service';
import { ChatMessage } from '../../core/models/chat.model';
import { environment } from '../../../environments/environment';
import { map } from 'rxjs';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss'],
  standalone: false
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesContainer') messagesContainer!: ElementRef;
  form!: FormGroup;
  messages: ChatMessage[] = [];
  sessionId: string | null = null;
  sending = false;
  projects: any[] = [];
  selectedProjectId: string | null = null;
  private shouldScroll = false;

  quickActions = [
    { label: 'Bug ISTQB',       prompt: 'Ayudame a estructurar un bug con principios ISTQB. El bug es: ' },
    { label: 'Caso ISO 29119',  prompt: 'Genera un caso de prueba en formato ISO 29119 para: ' },
    { label: 'Analizar Plan',   prompt: 'Analiza este plan de pruebas con ISO 29119 Parte 2: ' },
    { label: 'Tecnica ISTQB',   prompt: 'Que tecnica ISTQB me recomiendas para probar: ' },
    { label: 'Criterios Salida',prompt: 'Define criterios de salida ISTQB para: ' },
    { label: 'Smoke Test',      prompt: 'Define un smoke test para: ' },
  ];

  suggestions = [
    'Que es la particion de equivalencia?',
    'Como aplico ISO 29119?',
    'Explica el principio ISTQB de testing temprano',
    'Tecnicas de diseno de casos de prueba',
  ];

  constructor(
    private fb: FormBuilder,
    private chatService: ChatService,
    private http: HttpClient,
    private sanitizer: DomSanitizer,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      message: ['', [Validators.required, Validators.minLength(2)]]
    });
    this.loadProjects();
    this.addWelcomeMessage();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  addWelcomeMessage(): void {
    this.messages = [{
      id: 'welcome',
      role: 'assistant',
      content: 'Hola! Soy **QA Copilot**, tu asistente de Quality Assurance basado en estandares **ISTQB** e **ISO/IEC/IEEE 29119**. Puedo ayudarte con casos de prueba, analisis de planes, tecnicas de testing y mejores practicas.',
      sentAt: new Date().toISOString()
    }];
  }

  loadProjects(): void {
    this.http.get<any>(environment.apiUrl + '/api/projects').pipe(
      map(r => r.data || r)
    ).subscribe({
      next: (projects) => { this.projects = Array.isArray(projects) ? projects : []; },
      error: () => { this.projects = []; }
    });
  }

  onProjectChange(): void {
    this.sessionId = null;
    this.addWelcomeMessage();
    this.cdr.detectChanges();
  }

  clearChat(): void {
    this.sessionId = null;
    this.addWelcomeMessage();
    this.cdr.detectChanges();
  }

  useQuickAction(action: any): void {
    this.form.patchValue({ message: action.prompt });
  }

  sendMessage(): void {
    if (this.form.invalid || this.sending) return;
    const message = this.form.value.message.trim();
    if (!message) return;

    this.messages.push({
      id: Date.now().toString(),
      role: 'user',
      content: message,
      sentAt: new Date().toISOString()
    });
    this.form.reset();
    this.sending = true;
    this.shouldScroll = true;
    this.cdr.detectChanges();

    this.chatService.sendMessage({
      message,
      sessionId: this.sessionId || undefined,
      projectId: this.selectedProjectId || undefined
    }).subscribe({
      next: (response: any) => {
        this.sessionId = response.sessionId;
        const content = response.message || response.response || response.content || 'Sin respuesta';
        this.messages.push({
          id: Date.now().toString() + '_a',
          role: 'assistant',
          content,
          sentAt: new Date().toISOString()
        });
        this.sending = false;
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: () => {
        this.messages.push({
          id: 'err_' + Date.now(),
          role: 'assistant',
          content: 'Error al conectar con el asistente. Intenta de nuevo.',
          sentAt: new Date().toISOString()
        });
        this.sending = false;
        this.cdr.detectChanges();
      }
    });
  }

  askWhatIKnow(): void {
    const projectName = this.getProjectName();
    const prompt = `Resume todo lo que sabes sobre el proyecto "${projectName}". Incluye: documentos disponibles, procesos, estandares, plantillas y cualquier contexto relevante que tengas indexado. Si no tienes informacion especifica del proyecto, indicalo claramente.`;
    this.form.patchValue({ message: prompt });
    this.sendMessage();
  }

  getProjectName(): string {
    if (!this.selectedProjectId) return "Global";
    return this.projects.find(p => p.id === this.selectedProjectId)?.name || "Proyecto";
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  renderMarkdown(content: string): SafeHtml {
    if (!content) return this.sanitizer.bypassSecurityTrustHtml('');
    let html = content
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/^### (.+)$/gm, '<h3>$1</h3>')
      .replace(/^## (.+)$/gm, '<h2>$1</h2>')
      .replace(/^# (.+)$/gm, '<h1>$1</h1>')
      .replace(/^- (.+)$/gm, '<li>$1</li>')
      .replace(/(<li>.*<\/li>)/gs, '<ul>$1</ul>')
      .replace(/\n\n/g, '</p><p>')
      .replace(/\n/g, '<br>');
    return this.sanitizer.bypassSecurityTrustHtml('<p>' + html + '</p>');
  }

  formatTime(sentAt: string): string {
    if (!sentAt) return '';
    return new Date(sentAt).toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit' });
  }

  private scrollToBottom(): void {
    try {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
