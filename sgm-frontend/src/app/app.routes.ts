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
import { RegistrarDeudaEconomicaComponent } from './pages/deudas/registrar-deuda-economica/registrar-deuda-economica';
import { LiquidarDeudaEconomicaComponent } from './pages/deudas/liquidar-deuda-economica/liquidar-deuda-economica';
import { RegistrarDeudaSangreComponent } from './pages/deudas/registrar-deuda-sangre/registrar-deuda-sangre';
import { LiquidarDeudaSangreComponent } from './pages/deudas/liquidar-deuda-sangre/liquidar-deuda-sangre';
import { GestionarExoneracionComponent } from './pages/deudas/gestionar-exoneracion/gestionar-exoneracion';
import { ListaExpedientesLegales } from './pages/expediente-legal/lista-expedientes-legales/lista-expedientes-legales';
import { CrearExpedienteLegal } from './pages/expediente-legal/crear-expediente-legal/crear-expediente-legal';
import { DetalleExpedienteLegal } from './pages/expediente-legal/detalle-expediente-legal/detalle-expediente-legal';
import { ValidarAdmision } from './pages/expediente-legal/validar-admision/validar-admision';
import { AutorizarJefeGuardia } from './pages/expediente-legal/autorizar-jefe-guardia/autorizar-jefe-guardia';
import { BusquedaSalida } from './pages/vigilante/busqueda-salida/busqueda-salida';
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
      { path: 'busqueda-salida', component: BusquedaSalida },
      { path: 'deudas/registrar-economica', component: RegistrarDeudaEconomicaComponent},
      { path: 'deudas/liquidar-economica', component: LiquidarDeudaEconomicaComponent},
      { path: 'deudas/registrar-sangre', component: RegistrarDeudaSangreComponent},
      { path: 'deudas/liquidar-sangre', component: LiquidarDeudaSangreComponent},
      { path: 'deudas/gestionar-exoneracion', component: GestionarExoneracionComponent},
      { path: 'administrativo/legal/lista',component: ListaExpedientesLegales},
      { path: 'administrativo/legal/crear',component: CrearExpedienteLegal},
      { path: 'administrativo/legal/detalle/:id',component: DetalleExpedienteLegal},
      { path: 'administrativo/expedientes/validar-admision',component: ValidarAdmision},
      { path: 'administrativo/legal/autorizar-jefe-guardia', component: AutorizarJefeGuardia },
     

      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },

  // Redirección raíz global
  { path: '**', redirectTo: 'login' }
];
