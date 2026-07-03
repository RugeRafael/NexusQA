import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'teamFilter', standalone: false })
export class TeamFilterPipe implements PipeTransform {
  transform(team: any[], type: string): any {
    if (!team || team.length === 0) return 0;
    switch (type) {
      case 'enabled':
        return team.filter(u => u.indicatorsEnabled).length;
      case 'avgScore':
        const avg = team.reduce((s, u) => s + (u.scoreFinal || 0), 0) / team.length;
        return Math.round(avg);
      case 'projects':
        return team.reduce((s, u) => s + (u.proyectosAsignados || 0), 0);
      default:
        return 0;
    }
  }
}
