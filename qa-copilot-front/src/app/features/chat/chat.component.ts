import { Component, OnInit, ChangeDetectorRef, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ChatService } from '../../core/services/chat.service';
import { ChatMessage } from '../../core/models/chat.model';

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
  private shouldScroll = false;

  quickActions = [
    { label: '🐛 Estructurar Bug ISTQB',     prompt: 'Ayúdame a estructurar un bug con principios ISTQB. El bug es: ' },
    { label: '✅ Caso de Prueba ISO 29119',   prompt: 'Genera un caso de prueba en formato ISO 29119 para: ' },
    { label: '📋 Analizar Plan de Pruebas',   prompt: 'Analiza este plan de pruebas con ISO 29119 Parte 2: ' },
    { label: '🔍 Técnica de Diseño ISTQB',    prompt: '¿Qué técnica ISTQB me recomiendas para probar: ' },
    { label: '📊 Criterios de Salida',        prompt: 'Define criterios de salida ISTQB para: ' },
    { label: '⚡ Smoke Test',                  prompt: 'Define un smoke test para: ' },
  ];

  suggestions = [
    '¿Qué es la partición de equivalencia?',
    '¿Cómo aplico ISO 29119?',
    'Explica el principio ISTQB de testing temprano',
    'Técnicas de diseño de casos de prueba',
  ];

  constructor(
    private fb: FormBuilder,
    private chatService: ChatService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      message: ['', [Validators.required, Validators.minLength(2)]]
    });

    this.messages.push({
      id: 'welcome',
      role: 'assistant',
      content: `¡Hola! Soy **QA Copilot**, tu asistente de Quality Assurance basado en estándares **ISTQB** e **ISO/IEC/IEEE 29119**.

Puedo ayudarte con:
- 🐛 **Estructurar bugs** — incluso con información incompleta
- ✅ **Generar casos de prueba** en formato ISO 29119 Parte 3
- 📋 **Analizar planes de prueba** contra ISO 29119 Parte 2
- 🔍 **Aplicar técnicas** de diseño (partición equivalencia, valores límite, etc.)
- 📊 **Definir criterios** de entrada/salida de pruebas
- 💡 **Resolver dudas** sobre QA, testing y mejores prácticas

**Si me das información incompleta, no te rechazaré** — generaré lo que pueda con \`[PENDIENTE]\` y te diré qué falta. ¿En qué te ayudo?`,
      sentAt: new Date().toISOString()
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  useQuickAction(action: { label: string; prompt: string }): void {
    this.form.patchValue({ message: action.prompt });
    const textarea = document.querySelector('.chat-input textarea') as HTMLElement;
    textarea?.focus();
  }

  sendMessage(): void {
    if (this.form.invalid || this.sending) return;

    const message = this.form.value.message.trim();
    this.form.reset();

    this.messages.push({
      id: Date.now().toString(),
      role: 'user',
      content: message,
      sentAt: new Date().toISOString()
    });

    this.sending = true;
    this.shouldScroll = true;
    this.cdr.detectChanges();

    // Historial para contexto (últimos 10 mensajes, sin welcome)
    const sessionHistory = this.messages
      .filter(m => m.id !== 'welcome')
      .slice(-10)
      .map(m => ({ role: m.role, content: m.content }));

    const request = {
      message,
      sessionId: this.sessionId || undefined,
      session_history: sessionHistory
    };

    this.chatService.sendMessage(request).subscribe({
      next: (response) => {
        this.sessionId = response.sessionId;
        this.messages.push({
          id: Date.now().toString() + '_ai',
          role: 'assistant',
          content: response.response,
          sentAt: new Date().toISOString()
        });
        this.sending = false;
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: () => {
        this.sending = false;
        this.snackBar.open('Error al conectar con el asistente IA', 'Cerrar', { duration: 3000 });
        this.cdr.detectChanges();
      }
    });
  }

  clearChat(): void {
    this.sessionId = null;
    this.messages = [{
      id: 'welcome',
      role: 'assistant',
      content: '¡Chat reiniciado! ¿En qué puedo ayudarte con QA hoy?',
      sentAt: new Date().toISOString()
    }];
    this.cdr.detectChanges();
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    try {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }

  formatTime(date: string): string {
    return new Date(date).toLocaleTimeString('es-CO', {
      hour: '2-digit', minute: '2-digit'
    });
  }

  renderMarkdown(content: string): string {
    if (!content) return '';
    let html = content
      // Escapar HTML basico
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      // Bold
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      // Italic
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      // Inline code
      .replace(/`([^`]+)`/g, '<code class="inline-code">$1</code>')
      // Headers
      .replace(/^### (.+)$/gm, '<h4 class="md-h4">$1</h4>')
      .replace(/^## (.+)$/gm, '<h3 class="md-h3">$1</h3>')
      .replace(/^# (.+)$/gm, '<h2 class="md-h2">$1</h2>')
      // Listas con -
      .replace(/^- (.+)$/gm, '<li>$1</li>')
      // Listas numeradas
      .replace(/^\d+\. (.+)$/gm, '<li class="ol-li">$1</li>')
      // Saltos de linea dobles = parrafo
      .replace(/\n\n/g, '</p><p class="md-p">')
      // Saltos simples
      .replace(/\n/g, '<br>');

    // Envolver li en ul
    html = html.replace(/(<li>(?:(?!<li>).)*<\/li>)/gs, '<ul class="md-ul">$1</ul>');

    return `<p class="md-p">${html}</p>`;
  }
}
