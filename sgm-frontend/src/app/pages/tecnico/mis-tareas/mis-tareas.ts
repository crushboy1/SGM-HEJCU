import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';

import { ExpedienteService, Expediente } from '../../../services/expediente';
import { CustodiaService } from '../../../services/custodia';
import { AuthService } from '../../../services/auth';
import { IconComponent } from '../../../components/icon/icon.component';
import { getBadgeWithIcon } from '../../../utils/badge-styles';

@Component({
  selector: 'app-mis-tareas',
  standalone: true,
  imports: [CommonModule, IconComponent, FormsModule],
  templateUrl: './mis-tareas.html',
  styleUrl: './mis-tareas.css'
})
export class MisTareasComponent implements OnInit {
  private expedienteService = inject(ExpedienteService);
  private custodiaService = inject(CustodiaService);
  private authService = inject(AuthService);
  private router = inject(Router);

  // Datos
  pendientesRecojo: Expediente[] = [];
  enCustodia: Expediente[] = [];

  isLoading = true;
  activeTab: 'pendientes' | 'custodia' = 'pendientes';
  userName = '';

  ngOnInit() {
    this.userName = this.authService.getUserName();
    this.cargarDatos();
  }

  cargarDatos() {
    this.isLoading = true;

    this.expedienteService.getAll().subscribe({
      next: (data: Expediente[]) => {
        // 1. Pendientes de Recojo
        this.pendientesRecojo = data.filter((e: Expediente) => e.estadoActual === 'PendienteDeRecojo');

        // 2. En Mi Custodia
        this.enCustodia = data.filter((e: Expediente) =>
          e.estadoActual === 'EnTrasladoMortuorio' ||
          e.estadoActual === 'PendienteAsignacionBandeja'
        );

        this.isLoading = false;
      },
      error: (err: any) => { 
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  // --- ACCIÓN PRINCIPAL: ESCANEAR QR ---
  escanearQR() {
    Swal.fire({
      title: '📷 Escanear QR',
      text: 'Simulación: Ingrese el código del expediente o QR',
      input: 'text',
      inputPlaceholder: 'Ej: SGM-2025-00001',
      showCancelButton: true,
      confirmButtonText: 'Simular Escaneo',
      confirmButtonColor: '#0891B2',
      cancelButtonText: 'Cancelar',
      preConfirm: (codigo) => {
        if (!codigo) {
          Swal.showValidationMessage('Debe ingresar un código');
        }
        return codigo;
      }
    }).then((result) => {
      //Validación de tipos en el resultado de SweetAlert
      if (result.isConfirmed && result.value) {
        this.procesarTraspaso(result.value as string);
      }
    });
  }

  // Llamada al servicio para aceptar custodia
  procesarTraspaso(codigoQR: string) {
    Swal.fire({
      title: 'Procesando...',
      text: 'Validando código y aceptando custodia',
      allowOutsideClick: false,
      didOpen: () => Swal.showLoading()
    });

    this.custodiaService.realizarTraspaso({
      codigoQR: codigoQR,
      observaciones: 'Recojo registrado desde móvil'
    }).subscribe({
      next: (res: any) => { 
        Swal.fire({
          title: '¡Custodia Aceptada!',
          text: `Has recibido el cuerpo de: ${res.nombreCompleto}`,
          icon: 'success',
          timer: 2000,
          showConfirmButton: false
        });
        this.activeTab = 'custodia'; // Cambiar a tab de custodia
        this.cargarDatos(); // Recargar listas
      },
      error: (err: any) => { 
        console.error(err);
        Swal.fire('Error', err.error?.message || 'No se pudo realizar el traspaso', 'error');
      }
    });
  }

  // Navegar al mapa para asignar bandeja
  irAMapa() {
    this.router.navigate(['/mapa-mortuorio']);
  }

  // Helpers visuales
  getBadge(estado: string) {
    return getBadgeWithIcon(estado);
  }
}
