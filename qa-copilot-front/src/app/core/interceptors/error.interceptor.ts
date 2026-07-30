import { Injectable, ApplicationRef } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

const ERROR_MESSAGES: Record<string, string> = {
  AI_RATE_LIMIT: 'El documento es muy grande para la IA en este momento. Intenta con uno más corto o espera unos minutos.',
  AI_TIMEOUT: 'La IA tardó demasiado en responder. Intenta con un documento más corto o vuelve a intentarlo.',
  AI_UNAVAILABLE: 'El servicio de IA no está disponible en este momento. Intenta de nuevo más tarde.',
  NOT_FOUND: 'No se encontró el recurso solicitado.',
  FORBIDDEN: 'No tienes permisos para realizar esta acción.',
  UNAUTHORIZED: 'Tu sesión expiró. Inicia sesión de nuevo.',
  VALIDATION_ERROR: 'Revisa los datos ingresados e intenta de nuevo.',
  BAD_REQUEST: 'La solicitud no es válida.',
};

const SILENT_URL_PATTERNS: string[] = [
  '/api/auth/login',
];

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar,
    private appRef: ApplicationRef
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          this.authService.logout();
          this.router.navigate(['/auth/login']);
          return throwError(() => error);
        }

        const isSilent = SILENT_URL_PATTERNS.some(pattern => req.url.includes(pattern));
        if (!isSilent) {
          this.showErrorSnackbar(error);
        }

        return throwError(() => error);
      })
    );
  }

  private showErrorSnackbar(error: HttpErrorResponse): void {
    const errorCode: string | undefined = error.error?.errorCode;
    const backendMessage: string | undefined = error.error?.message;

    let message: string;
    if (errorCode && ERROR_MESSAGES[errorCode]) {
      message = ERROR_MESSAGES[errorCode];
    } else if (backendMessage) {
      message = backendMessage;
    } else if (error.status === 0) {
      message = 'No se pudo conectar con el servidor. Revisa tu conexión.';
    } else {
      message = 'Ocurrió un error inesperado. Intenta de nuevo.';
    }

    const isWarning = errorCode === 'AI_RATE_LIMIT' || errorCode === 'AI_UNAVAILABLE' || errorCode === 'AI_TIMEOUT';

    this.snackBar.open(message, 'Cerrar', {
      duration: isWarning ? 7000 : 4000,
      panelClass: isWarning ? ['snackbar-warning'] : ['snackbar-error']
    });

    // CRITICO: la app corre en modo zoneless (sin zone.js).
    // El interceptor vive fuera del ciclo de deteccion de cambios de cualquier
    // componente, asi que sin este tick manual, el overlay del snackbar se crea
    // en memoria pero nunca se pinta en el DOM.
    this.appRef.tick();
  }
}
