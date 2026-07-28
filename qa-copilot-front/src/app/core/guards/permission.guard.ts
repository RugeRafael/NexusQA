import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { PermissionsService } from '../services/permissions.service';

@Injectable({ providedIn: 'root' })
export class PermissionGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private permissionsService: PermissionsService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    // Si la ruta define moduleKey, verificar en permisos del servidor
    const moduleKey = route.data['moduleKey'] as string;
    if (moduleKey) {
      const allowed = this.permissionsService.hasModule(moduleKey);
      if (!allowed) {
        this.router.navigate(['/dashboard']);
        return false;
      }
    }

    return true;
  }
}
