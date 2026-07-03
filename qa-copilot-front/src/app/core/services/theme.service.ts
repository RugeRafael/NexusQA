import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private isDark = new BehaviorSubject<boolean>(this.getStoredTheme());
  isDark$ = this.isDark.asObservable();

  constructor() {
    this.applyTheme(this.isDark.value);
  }

  toggle(): void {
    const newValue = !this.isDark.value;
    this.isDark.next(newValue);
    localStorage.setItem('qa_copilot_theme', newValue ? 'dark' : 'light');
    this.applyTheme(newValue);
  }

  private applyTheme(dark: boolean): void {
    const body = document.body;
    if (dark) {
      body.classList.add('dark-theme');
    } else {
      body.classList.remove('dark-theme');
    }
  }

  private getStoredTheme(): boolean {
    return localStorage.getItem('qa_copilot_theme') === 'dark';
  }

  get currentTheme(): boolean {
    return this.isDark.value;
  }
}