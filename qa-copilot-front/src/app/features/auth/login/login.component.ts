import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  standalone: false
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  hidePassword = true;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }

    this.loginForm = this.fb.group({
      email: ['', [
        Validators.required,
        Validators.email,
        Validators.pattern(/^[a-zA-Z0-9._%+-]+@ithealth\.co$/)
      ]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
  if (this.loginForm.invalid) return;
  this.isLoading = true;
  this.errorMessage = '';

  this.authService.login(this.loginForm.value).subscribe({
   next: (response) => {
  this.isLoading = false;
  this.snackBar.open(`Bienvenido, ${response.userName}`, 'Cerrar', { duration: 3000 });
  setTimeout(() => {
    this.router.navigate(['/dashboard']);
  }, 200);
},
    error: (error) => {
      this.isLoading = false;
      this.errorMessage = error.error?.message || 'Error al iniciar sesión.';
    }
  });
}

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}