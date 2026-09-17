import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth.service';

@Component({ selector: 'sv-login', standalone: true, imports: [CommonModule, ReactiveFormsModule], templateUrl: './login.component.html', styleUrl: './login.component.scss' })
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly form = this.fb.nonNullable.group({ tenantId: ['', [Validators.required]], email: ['', [Validators.required, Validators.email]], senha: ['', [Validators.required, Validators.minLength(6)]] });
  loading = false;
  error = '';
  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true; this.error = '';
    const { tenantId, email, senha } = this.form.getRawValue();
    this.auth.login(tenantId, email, senha).pipe(finalize(() => this.loading = false)).subscribe({ next: () => void this.router.navigateByUrl('/dashboard'), error: () => this.error = 'Não foi possível autenticar. Verifique os dados e tente novamente.' });
  }
}
