import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

// Models & Helpers
import {
  ExpedienteLegalDTO,
  TimelineEstadoExpediente,
  generarTimelineExpedienteLegal
} from '../../models/expediente-legal.model';

// Components
import { IconComponent } from '../icon/icon.component';

/**
 * TimelineEstados Component
 * * Visualiza el flujo de estados del expediente legal (Híbrido).
 * Muestra: Estado, Fecha, Responsable y Observaciones de cada etapa.
 * * Uso: <app-timeline-estados [expediente]="expedienteDto"></app-timeline-estados>
 */
@Component({
  selector: 'app-timeline-estados',
  standalone: true,
  imports: [CommonModule, IconComponent],
  templateUrl: './timeline-estados.html',
  styleUrl: './timeline-estados.css'
})
export class TimelineEstados implements OnChanges {
  // ===================================================================
  // INPUTS
  // ===================================================================
  @Input({ required: true }) expediente!: ExpedienteLegalDTO;

  // ===================================================================
  // DATOS
  // ===================================================================
  timeline: TimelineEstadoExpediente[] = [];

  // ===================================================================
  // CICLO DE VIDA
  // ===================================================================
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['expediente'] && this.expediente) {
      this.generarTimeline();
    }
  }

  // ===================================================================
  // LÓGICA PRIVADA
  // ===================================================================
  private generarTimeline(): void {
    // Usamos el helper definido en el modelo para garantizar consistencia
    this.timeline = generarTimelineExpedienteLegal(this.expediente);
  }
}
