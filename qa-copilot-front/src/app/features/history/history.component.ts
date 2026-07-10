import { Component, OnInit, ChangeDetectorRef, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { HttpClient } from '@angular/common/http';
import { TestcaseService } from '../../core/services/testcase.service';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-history',
  templateUrl: './history.component.html',
  styleUrls: ['./history.component.scss'],
  standalone: false
})
export class HistoryComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  history: any[] = [];
  allHistory: any[] = [];
  loading = false;
  totalItems = 0;
  pageSize = 10;
  currentPage = 1;
  selectedItem: any = null;
  viewMode: 'mine' | 'all' = 'mine';
  isAdminOrSenior = false;
  selectedUserEmail = '';
  userList: { name: string, email: string }[] = [];
  displayedColumns = ['documentName', 'totalTestCases', 'confidenceScore', 'status', 'generatedAt', 'actions'];

  constructor(
    private testcaseService: TestcaseService,
    private authService: AuthService,
    private http: HttpClient,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const role = this.authService.getUserRole();
    this.isAdminOrSenior = role === 'Admin' || role === 'Senior';
    this.loadHistory();
  }

  onViewModeChange(): void {
    this.selectedUserEmail = '';
    this.userList = [];
    this.currentPage = 1;
    if (this.viewMode === 'all') {
      this.displayedColumns = ['documentName', 'userName', 'totalTestCases', 'confidenceScore', 'status', 'generatedAt', 'actions'];
      this.loadAllHistory();
    } else {
      this.displayedColumns = ['documentName', 'totalTestCases', 'confidenceScore', 'status', 'generatedAt', 'actions'];
      this.loadHistory();
    }
  }

  onUserFilterChange(): void {
    if (this.selectedUserEmail) {
      this.history = this.allHistory.filter(h => h.userEmail === this.selectedUserEmail);
    } else {
      this.history = this.allHistory;
    }
    this.totalItems = this.history.length;
    this.cdr.detectChanges();
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
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  loadAllHistory(): void {
    this.loading = true;
    this.http.get<any>(`${environment.apiUrl}/api/history/all?pageSize=100`).subscribe({
      next: (res) => {
        const data = res.data || res;
        this.allHistory = data.items || data;
        this.history = this.allHistory;
        this.totalItems = data.total || this.history.length;
        this.buildUserList();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  private buildUserList(): void {
    const map = new Map<string, { name: string, email: string }>();
    this.allHistory.forEach((h: any) => {
      if (h.userEmail && !map.has(h.userEmail)) {
        map.set(h.userEmail, { name: h.userName || h.userEmail, email: h.userEmail });
      }
    });
    this.userList = Array.from(map.values());
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    if (this.viewMode === 'mine') this.loadHistory();
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
      'Completed': '#10B981', 'Generating': '#F59E0B',
      'Failed': '#EF4444', 'Pending': '#64748B'
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
