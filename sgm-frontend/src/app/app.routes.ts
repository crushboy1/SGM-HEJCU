import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login';
import { MainLayoutComponent } from './layout/main-layout/main-layout';
import { DashboardComponent } from './pages/dashboard/dashboard';
import { BandejaUniversalComponent } from './pages/bandeja-universal/bandeja-universal';
import { MisExpedientesComponent } from './pages/mis-expedientes/mis-expedientes';
import { ExpedienteCreateComponent } from './pages/expediente-create/expediente-create';
import { MapaMortuorioComponent } from './pages/mapa-mortuorio/mapa-mortuorio';
import { VerificacionIngresoComponent } from './pages/vigilante/verificacion-ingreso/verificacion-ingreso';
import { AsignarBandejaComponent } from './pages/asignar-bandeja/asignar-bandeja';
import { MisTareasComponent } from './pages/tecnico/mis-tareas/mis-tareas';
import { RegistroSalidaComponent } from './pages/vigilante/registro-salida/registro-salida';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  // RUTA PÚBLICA
  {
    path: 'login',
    component: LoginComponent
  },

  // RUTAS PROTEGIDAS
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: DashboardComponent },
      { path: 'bandeja-entrada', component: BandejaUniversalComponent },
      { path: 'nuevo-expediente/:hc', component: ExpedienteCreateComponent },
      { path: 'mis-expedientes', component: MisExpedientesComponent },
      { path: 'nuevo-expediente', component: ExpedienteCreateComponent },
      { path: 'mapa-mortuorio', component: MapaMortuorioComponent },
      { path: 'verificacion-ingreso', component: VerificacionIngresoComponent },
      { path: 'asignar-bandeja/:id', component: AsignarBandejaComponent },
      { path: 'mis-tareas', component: MisTareasComponent },
      { path: 'registro-salida', component: RegistroSalidaComponent },

      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  // Redirección raíz global
  { path: '**', redirectTo: 'login' }
];
