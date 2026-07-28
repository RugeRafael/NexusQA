import { Component, OnInit, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpClient } from '@angular/common/http';
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
    { label: "Bug ISTQB",     prompt: "Ayudame a estructurar un bug con principios ISTQB. El bug es: " },
    { label: "Caso ISO 29119",   prompt: "Genera un caso de prueba en formato ISO 29119 para: " },
    { label: "Analizar Plan",   prompt: "Analiza este plan de pruebas con ISO 29119 Parte 2: " },
    { label: "Tecnica ISTQB",    prompt: "Que tecnica ISTQB me recomiendas para probar: " },
    { label: "Criterios Salida",        prompt: "Define criterios de salida ISTQB para: " },
    { label: "Smoke Test",                  prompt: "Define un smoke test para: " },
  ];

  suggestions = [
    "Que es la particion de equivalencia?",
    "Como aplico ISO 29119?",
    "Explica el principio ISTQB de testing temprano",
    "Tecnicas de diseno de casos de prueba",
  ];

  constructor(
    private fb: FormBuilder,
    private chatService: ChatService,
    private http: HttpClient,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      message: ['', [Validators.required, Validators.minLength(2)]]
    });
    this.loadProjects();
    this.messages.push({
      id: 'welcome',
      role: 'assistant',
      content: 'Hola! Soy QA Copilot, tu asistente de Quality Assurance basado en estandares ISTQB e ISO/IEC/IEEE 29119. Puedo ayudarte con casos de prueba, analisis de planes, tecnicas de testing y mejores practicas. Selecciona un proyecto para usar el contexto especifico de ithealth.',
      sentAt: new Date().toISOString()
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
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
    this.messages = [{
      id: 'welcome',
      role: 'assistant',
      content: this.selectedProjectId
        ? 'Proyecto seleccionado. Ahora usare el contexto especifico de este proyecto al responder.'
        : 'Modo global activado. Usare el contexto general de ithealth.',
      sentAt: new Date().toISOString()
    }];
    this.cdr.detectChanges();
  }

  sendMessage(): void {
    if (this.form.invalid || this.sending) return;
    const message = this.form.value.message.trim();
    if (!message) return;

    this.messages.push({ id: Date.now().toString(), role: 'user', content: message, sentAt: new Date().toISOString() });
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
        this.messages.push({ id: Date.now().toString() + '_a', role: 'assistant', content, sentAt: new Date().toISOString() });
        this.sending = false;
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: () => {
        this.messages.push({ id: 'err', role: 'assistant', content: 'Error al conectar con el asistente. Intenta de nuevo.', sentAt: new Date().toISOString() });
        this.sending = false;
        this.cdr.detectChanges();
      }
    });
  }

  useQuickAction(prompt: string): void {
    this.form.patchValue({ message: prompt });
  }

  useSuggestion(suggestion: string): void {
    this.form.patchValue({ message: suggestion });
    this.sendMessage();
  }

  private scrollToBottom(): void {
    try {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }

  getProjectName(): string {
    if (!this.selectedProjectId) return "Global";
    return this.projects.find(p => p.id === this.selectedProjectId)?.name || "Proyecto";
  }
}
