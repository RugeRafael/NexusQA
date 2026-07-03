import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private notificationHub?: signalR.HubConnection;
  private projectHub?: signalR.HubConnection;
  private activityHub?: signalR.HubConnection;

  projectAssigned$ = new BehaviorSubject<any>(null);
  notification$ = new BehaviorSubject<any>(null);
  teamActivity$ = new BehaviorSubject<any>(null);

  constructor(private authService: AuthService) {}

  async startConnection(): Promise<void> {
    const token = this.authService.getToken();
    if (!token) return;

    this.notificationHub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/hubs/notifications`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.projectHub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/hubs/projects`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.activityHub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.signalRUrl}/hubs/activity`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.notificationHub.on('ReceiveNotification', (notification) => {
      this.notification$.next(notification);
    });

    this.notificationHub.on('ProjectAssigned', (notification) => {
      this.projectAssigned$.next(notification);
    });

    this.activityHub.on('TeamActivity', (activity) => {
      this.teamActivity$.next(activity);
    });

    try {
      await this.notificationHub.start();
      await this.projectHub.start();
      await this.activityHub.start();
    } catch (err) {
      console.error('SignalR connection error:', err);
    }
  }

  async stopConnection(): Promise<void> {
    await this.notificationHub?.stop();
    await this.projectHub?.stop();
    await this.activityHub?.stop();
  }
}