import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'jiraFilter', standalone: false })
export class JiraFilterPipe implements PipeTransform {

  private readonly DONE = ['Finalizada', 'Exitoso', 'Done', 'Finalizado'];
  private readonly PENDING = ['Por hacer', 'Bloqueado', 'En progreso'];

  transform(bugs: any[], type: string): any {
    if (!bugs) return type.startsWith('list:') ? [] : 0;

    if (type === 'exitosos') return bugs.filter(b => this.DONE.includes(b.status)).length;
    if (type === 'bugs') return bugs.filter(b => b.issueType?.toLowerCase().includes('bug') && !this.DONE.includes(b.status) && b.status !== 'Cancelado').length;
    if (type === 'pendientes') return bugs.filter(b => this.PENDING.includes(b.status)).length;
    if (type === 'cancelados') return bugs.filter(b => b.status === 'Cancelado').length;
    if (type === 'list:bugs') return bugs.filter(b => b.issueType?.toLowerCase().includes('bug') && !this.DONE.includes(b.status) && b.status !== 'Cancelado');
    if (type === 'list:pendientes') return bugs.filter(b => this.PENDING.includes(b.status));
    if (type === 'list:exitosos') return bugs.filter(b => this.DONE.includes(b.status));
    if (type === 'list:all') return [...bugs];
    return bugs.length;
  }
}
