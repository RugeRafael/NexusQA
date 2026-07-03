import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'jiraFilter', standalone: false })
export class JiraFilterPipe implements PipeTransform {
  transform(bugs: any[], type: string): number {
    if (!bugs) return 0;
    switch (type) {
      case 'exitosos':
        return bugs.filter(b => ['Finalizada','Exitoso','Done','Finalizado'].includes(b.status)).length;
      case 'bugs':
        return bugs.filter(b => b.issueType?.includes('Bug') &&
          !['Finalizada','Exitoso','Done','Finalizado','Cancelado'].includes(b.status)).length;
      case 'pendientes':
        return bugs.filter(b => ['Por hacer','Bloqueado','En progreso'].includes(b.status)).length;
      case 'cancelados':
        return bugs.filter(b => b.status === 'Cancelado').length;
      default: return bugs.length;
    }
  }
}