import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationsService } from '../../services/notifications.service';
import { ThemeService } from '../../core/theme.service';
import { AuthService } from '../../core/auth.service';

const nonBlank = (control: AbstractControl) => control.value?.trim() ? null : { required: true };

@Component({
  selector: 'sv-create-notification', standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './create-notification.component.html',
  styleUrl: './create-notification.component.scss'
})
export class CreateNotificationComponent {
  private readonly notifications = inject(NotificationsService);
  readonly theme = inject(ThemeService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  readonly form = this.fb.nonNullable.group({
    destinatario: ['', [Validators.required, nonBlank]],
    canal: [1], prioridade: [2],
    assunto: ['', Validators.maxLength(200)],
    conteudo: ['', [Validators.required, nonBlank, Validators.maxLength(4000)]]
  }, { validators: control => {
    if (control.get('canal')?.value !== 1) return null;
    return !control.get('assunto')?.value?.trim() ? { subjectRequired: true }
      : !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(control.get('destinatario')?.value?.trim()) ? { emailInvalid: true } : null;
  }});
  loading = false;
  createdId = '';
  error = '';

  submit(): void {
    if (this.loading || this.createdId) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const values = this.form.getRawValue();
    this.loading = true;
    this.error = '';
    this.notifications.create({ ...values, destinatario: values.destinatario.trim(),
      assunto: values.assunto.trim() || null, aplicacaoId: crypto.randomUUID()
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading = false)).subscribe({
      next: response => this.createdId = response.id,
      error: (error: HttpErrorResponse) => this.error = error.status === 0
        ? 'A conexão foi interrompida. Confira o inbox antes de reenviar: a notificação pode ter sido criada.'
        : typeof error.error?.detail === 'string' ? error.error.detail : 'Não foi possível criar a notificação. Confira os dados e tente novamente.'
    });
  }

  newNotification(): void {
    this.createdId = '';
    this.error = '';
    this.form.reset({ canal: 1, prioridade: 2 });
  }

  logout(): void { this.auth.logout(); }
}
