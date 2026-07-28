import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

const CACHE_KEY = 'nx_modules';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly apiUrl = environment.apiUrl + '/api/permissions';
  private cachedModules: string[] = [];

  constructor(private http: HttpClient) {}

  getMyModules(): Observable<string[]> {
    // Usar caché de localStorage para carga instantánea
    const cached = localStorage.getItem(CACHE_KEY);
    if (cached) {
      try {
        this.cachedModules = JSON.parse(cached);
        return of(this.cachedModules);
      } catch { localStorage.removeItem(CACHE_KEY); }
    }

    return this.http.get<any>(this.apiUrl + '/my-modules').pipe(
      map(r => r.data?.modules || []),
      tap(modules => {
        this.cachedModules = modules;
        localStorage.setItem(CACHE_KEY, JSON.stringify(modules));
      })
    );
  }

  getCachedModules(): string[] {
    return this.cachedModules;
  }

  hasModule(moduleKey: string): boolean {
    return this.cachedModules.includes(moduleKey);
  }

  getAllPermissions(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl).pipe(map(r => r.data || []));
  }

  updateRolePermissions(role: string, modules: { moduleKey: string; isEnabled: boolean }[]): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${role}`, { modules }).pipe(map(r => r.data || r));
  }

  // Llamar al hacer logout para limpiar caché
  clearCache(): void {
    localStorage.removeItem(CACHE_KEY);
    this.cachedModules = [];
  }

  // Llamar cuando Admin cambia permisos para forzar recarga
  refreshModules(): Observable<string[]> {
    localStorage.removeItem(CACHE_KEY);
    return this.getMyModules();
  }
}
