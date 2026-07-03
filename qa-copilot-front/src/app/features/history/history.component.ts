import { Component, OnInit, ChangeDetectorRef, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { TestcaseService } from '../../core/services/testcase.service';

@Component({
  selector: 'app-history',
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.scss'],
  standalone: false
})
export class HistoryComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  history: any[] = [];
  loading = false;
  totalItems = 0;
  pageSize = 10;
  currentPage = 1;
  selectedItem: any = null;
  displayedColumns = ['documentName', 'totalTestCases', 'confidenceScore', 'status', 'generatedAt', 'actions'];

  constructor(
    private testcaseService: TestcaseService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.loading = true;
    this.testcaseService.getHistory(this.currentPage, this.pageSize).subscribe({
      next: (data) => {
        this.history = data.items || [];
        this.totalItems = data.totalItems || 0;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadHistory();
  }

  viewDetails(item: any): void {
    this.selectedItem = this.selectedItem?.id === item.id ? null : item;
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString('es-CO', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  getStatusColor(status: string): string {
    const colors: Record<string, string> = {
      'Completed': '#10B981',
      'Generating': '#F59E0B',
      'Failed': '#EF4444',
      'Pending': '#64748B'
    };
    return colors[status] || '#64748B';
  }

  getConfidenceColor(score: number): string {
    const pct = score * 100;
    if (pct >= 80) return '#10B981';
    if (pct >= 60) return '#F59E0B';
    return '#EF4444';
  }
}