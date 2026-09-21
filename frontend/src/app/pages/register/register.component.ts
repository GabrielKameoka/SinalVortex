import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'sv-register', standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['../login/login.component.scss', './register.component.scss']
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(200), (control: AbstractControl) => control.value?.trim() ? null : { required: true }]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    senha: ['', [Validators.required, Validators.minLength(12), Validators.maxLength(128)]],
    confirmacao: ['', Validators.required]
  }, { validators: (control: AbstractControl) => control.get('senha')?.value === control.get('confirmacao')?.value ? null : { mismatch: true } });
  loading = false;
  error = '';
  tenantId: string | null = null;
  copyMessage = '';

  submit(): void {
    if (this.loading || this.tenantId) return;
    this.form.patchValue({ nome: this.form.controls.nome.value.trim(), email: this.form.controls.email.value.trim() });
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.error = '';
    this.loading = true;
    const { nome, email, senha } = this.form.getRawValue();
    this.auth.register({ nome, email, senha }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.loading = false)
    ).subscribe({
      next: response => { this.tenantId = response.tenantId; this.form.reset(); },
      error: (error: HttpErrorResponse) => {
        this.error = error.status === 0
          ? 'Não foi possível conectar à API. Confira sua conexão. Se o envio foi interrompido, a conta pode ter sido criada.'
          : error.status === 400
            ? 'Confira os dados informados. ' + (typeof error.error?.detail === 'string' ? error.error.detail : '')
            : 'Não foi possível concluir o cadastro. Tente novamente mais tarde.';
      }
    });
  }

  async copyTenant(): Promise<void> {
    if (!this.tenantId) return;
    try {
      await navigator.clipboard.writeText(this.tenantId);
      this.copyMessage = 'Tenant ID copiado.';
    } catch {
      this.copyMessage = 'Não foi possível copiar. Selecione e copie o Tenant ID exibido.';
    }
  }
}

