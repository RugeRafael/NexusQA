import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-recent-activity',
  templateUrl: './recent-activity.component.html',
  styleUrls: ['./recent-activity.component.scss'],
  standalone: false
})
export class RecentActivityComponent {
  @Input() activities: any[] = [];
  @Input() loading = false;

  getModuleIcon(module: string): string {
    const icons: Record<string, string> = {
      'Documents': 'description',
      'TestCases': 'auto_awesome',
      'Auth': 'lock',
      'Metrics': 'bar_chart',
      'Projects': 'work',
      'Chat': 'smart_toy',
    };
    return icons[module] || 'circle';
  }

  getModuleColor(module: string): string {
    const colors: Record<string, string> = {
      'Documents': '#3B82F6',
      'TestCases': '#10B981',
      'Auth': '#EF4444',
      'Metrics': '#F59E0B',
      'Projects': '#8B5CF6',
      'Chat': '#06B6D4',
    };
    return colors[module] || '#64748B';
  }

 formatTime(date: string): string {
  if (!date) return 'Ahora';
  return new Date(date).toLocaleString('es-CO', {
    hour: '2-digit', minute: '2-digit',
    day: '2-digit', month: 'short'
  });
}
}