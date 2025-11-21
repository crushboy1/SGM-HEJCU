import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { BandejaService, Bandeja } from '../../services/bandeja';
import { IconComponent } from '../../components/icon/icon.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-asignar-bandeja',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent],
  templateUrl: './asignar-bandeja.html'
})
export class AsignarBandejaComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private bandejaService = inject(BandejaService);

  bandejaId: number = 0;
  bandeja: Bandeja | null = null;
  isLoading = true;
  isSaving = false;

  // Formulario
  codigoExpediente: string = '';
  observaciones: string = '';

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.bandejaId = +id;
      this.cargarInfoBandeja();
    } else {
      this.router.navigate(['/mapa-mortuorio']);
    }
  }

  cargarInfoBandeja() {
    this.bandejaService.getById(this.bandejaId).subscribe({
      next: (data) => {
        this.bandeja = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        Swal.fire('Error', 'No se pudo cargar la información de la bandeja', 'error');
        this.router.navigate(['/mapa-mortuorio']);
      }
    });
  }

  asignar() {
    if (!this.codigoExpediente) {
      Swal.fire('Falta datos', 'Ingrese el código del expediente a asignar', 'warning');
      return;
    }

    this.isSaving = true;

    // NOTA: Para la demo, asumimos que el usuario ingresa el ID numérico del expediente
    // En un flujo real, buscaríamos por código "SGM-..." y obtendríamos el ID.
    // Aquí intentaremos parsear, si no, enviamos 0 (fallará en backend pero sirve para validar flujo UI)
    const expedienteId = parseInt(this.codigoExpediente) || 0;
    if (isNaN(expedienteId)) {
      Swal.fire('Error', 'Para esta demo, por favor ingrese el ID numérico del expediente (ej: 8)', 'info');
      this.isSaving = false;
      return;
    }

    this.bandejaService.asignar({
      bandejaID: this.bandejaId,
      expedienteID: expedienteId,
      observaciones: this.observaciones
    }).subscribe({
      next: () => {
        this.isSaving = false;
        Swal.fire({
          title: 'Asignado',
          text: `Cuerpo asignado correctamente a la bandeja ${this.bandeja?.codigo}`,
          icon: 'success',
          timer: 2000
        }).then(() => this.router.navigate(['/mapa-mortuorio']));
      },
      error: (err) => {
        this.isSaving = false;
        console.error(err);
        Swal.fire('Error', err.error?.message || 'No se pudo realizar la asignación', 'error');
      }
    });
  }

  cancelar() {
    this.router.navigate(['/mapa-mortuorio']);
  }
}
