import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface HealthCheckResponse {
  status: string;
  timestamp: string;
}

@Injectable({
  providedIn: 'root'
})
export class HealthService {
  private apiUrl = 'http://localhost:5287/api/health';

  constructor(private http: HttpClient) { }

  getHealth(): Observable<HealthCheckResponse> {
    return this.http.get<HealthCheckResponse>(this.apiUrl);
  }
}
