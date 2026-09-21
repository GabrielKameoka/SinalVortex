import { CreateNotificationComponent } from './pages/create-notification/create-notification.component';
import { Routes, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './core/auth.service';
import { RegisterComponent } from './pages/register/register.component';
import { authGuard } from './core/auth.guard';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { InboxComponent } from './pages/inbox/inbox.component';
import { QueueMonitorComponent } from './pages/queue-monitor/queue-monitor.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'registro', component: RegisterComponent, canActivate: [() => inject(AuthService).isAuthenticated() ? inject(Router).createUrlTree(['/dashboard']) : true] },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'nova-notificacao', component: CreateNotificationComponent, canActivate: [authGuard] },
  { path: 'inbox', component: InboxComponent, canActivate: [authGuard] },
  { path: 'monitor-filas', component: QueueMonitorComponent, canActivate: [authGuard] },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' }
];
