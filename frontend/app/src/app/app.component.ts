import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HealthService, HealthCheckResponse } from './services/health.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'SinalVortex';
  healthStatus: HealthCheckResponse | null = null;
  isLoading = true;
  error: string | null = null;

  constructor(private healthService: HealthService) {}

  ngOnInit() {
    this.checkHealth();
  }

  checkHealth() {
    this.isLoading = true;
    this.error = null;
    this.healthService.getHealth().subscribe({
      next: (response) => {
        this.healthStatus = response;
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Erro ao conectar com o backend: ' + err.message;
        this.isLoading = false;
        console.error('Error:', err);
      }
    });
  }
}
