import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Componente de Íconos SVG
 * Sistema centralizado de íconos basado en Lucide Icons
 * 
 * @version 2.0.0
 * @changelog
 * - v2.0.0: Agregados íconos 'ban', 'settings', 'filter', 'download', 'plus', 'minus', 'edit', 'trash'
 * 
 * Uso:
 * <app-icon name="inbox" [size]="24"></app-icon>
 * 
 * Para aplicar color, envolver en un div o usar clase directa:
 * <div class="text-blue-500">
 *   <app-icon name="inbox" [size]="24"></app-icon>
 * </div>
 * 
 * Íconos disponibles (53):
 * - Navegación: home, menu, log-out, x, chevron-*
 * - Archivos: file-plus, file-text, file-check, file-badge, folder, archive
 * - Alertas: alert-triangle, alert-circle, circle-check, circle-x, info
 * - Acciones: zap, refresh-cw, search, eye, printer, filter, download, upload, plus, minus, edit, trash, loader
 * - Tiempo: clock, calendar
 * - Usuarios: user, users
 * - Identificadores: hash, qr-code
 * - Financiero: credit-card
 * - Hospital: droplet, building-2, truck, map-pin
 * - Configuración: settings, ban
 * - Administrativo: bell, book, shield, bar-chart
 */
@Component({
  selector: 'app-icon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <svg 
      xmlns="http://www.w3.org/2000/svg" 
      [attr.width]="size" 
      [attr.height]="size" 
      viewBox="0 0 24 24" 
      fill="none" 
      stroke="currentColor" 
      stroke-width="2" 
      stroke-linecap="round" 
      stroke-linejoin="round"
      style="display: block;">
      <ng-container [ngSwitch]="name">
        
        <!-- ============================================
             NAVEGACIÓN Y LAYOUT
             ============================================ -->
        
        <g *ngSwitchCase="'home'">
          <path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
          <polyline points="9 22 9 12 15 12 15 22"></polyline>
        </g>
        
        <g *ngSwitchCase="'menu'">
          <line x1="4" x2="20" y1="12" y2="12"></line>
          <line x1="4" x2="20" y1="6" y2="6"></line>
          <line x1="4" x2="20" y1="18" y2="18"></line>
        </g>
        
        <g *ngSwitchCase="'log-out'">
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
          <polyline points="16 17 21 12 16 7"></polyline>
          <line x1="21" x2="9" y1="12" y2="12"></line>
        </g>
        
        <g *ngSwitchCase="'x'">
          <path d="M18 6 6 18"></path>
          <path d="m6 6 12 12"></path>
        </g>
        
        <!-- ============================================
             NAVEGACIÓN DE PAGINACIÓN
             ============================================ -->
        
        <g *ngSwitchCase="'chevron-left'">
          <path d="m15 18-6-6 6-6"></path>
        </g>
        
        <g *ngSwitchCase="'chevron-right'">
          <path d="m9 18 6-6-6-6"></path>
        </g>
        
        <g *ngSwitchCase="'chevron-up'">
          <path d="m18 15-6-6-6 6"></path>
        </g>

        <g *ngSwitchCase="'chevron-down'">
          <path d="m6 9 6 6 6-6"></path>
        </g>
        
        <!-- ============================================
             BANDEJA Y TAREAS
             ============================================ -->
        
        <g *ngSwitchCase="'inbox'">
          <polyline points="22 12 16 12 14 15 10 15 8 12 2 12"></polyline>
          <path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"></path>
        </g>
        
        <g *ngSwitchCase="'clipboard-list'">
          <rect width="8" height="4" x="8" y="2" rx="1" ry="1"></rect>
          <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"></path>
          <path d="M12 11h4"></path>
          <path d="M12 16h4"></path>
          <path d="M8 11h.01"></path>
          <path d="M8 16h.01"></path>
        </g>
        
        <!-- ============================================
             ARCHIVOS Y DOCUMENTOS
             ============================================ -->
        
        <g *ngSwitchCase="'file-plus'">
          <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"></path>
          <polyline points="14 2 14 8 20 8"></polyline>
          <line x1="12" x2="12" y1="18" y2="12"></line>
          <line x1="9" x2="15" y1="15" y2="15"></line>
        </g>
        
        <g *ngSwitchCase="'file-text'">
          <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"></path>
          <polyline points="14 2 14 8 20 8"></polyline>
          <line x1="16" x2="8" y1="13" y2="13"></line>
          <line x1="16" x2="8" y1="17" y2="17"></line>
          <polyline points="10 9 9 9 8 9"></polyline>
        </g>
        
        <g *ngSwitchCase="'file-check'">
          <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"></path>
          <polyline points="14 2 14 8 20 8"></polyline>
          <path d="m9 15 2 2 4-4"></path>
        </g>
        
        <g *ngSwitchCase="'folder'">
          <path d="M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z"></path>
        </g>
        
        <g *ngSwitchCase="'archive'">
          <rect width="20" height="5" x="2" y="3" rx="1"></rect>
          <path d="M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8"></path>
          <path d="M10 12h4"></path>
        </g>
        
        <!-- ============================================
             ALERTAS Y ESTADOS
             ============================================ -->
        
        <g *ngSwitchCase="'alert-triangle'">
          <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"></path>
          <path d="M12 9v4"></path>
          <path d="M12 17h.01"></path>
        </g>
        
        <g *ngSwitchCase="'alert-circle'">
          <circle cx="12" cy="12" r="10"></circle>
          <line x1="12" x2="12" y1="8" y2="12"></line>
          <line x1="12" x2="12.01" y1="16" y2="16"></line>
        </g>
        
        <ng-container *ngIf="name === 'circle-check' || name === 'check-circle'">
          <circle cx="12" cy="12" r="10"></circle>
          <path d="m9 12 2 2 4-4"></path>
        </ng-container>
        
        <g *ngSwitchCase="'circle-x'">
          <circle cx="12" cy="12" r="10"></circle>
          <path d="m15 9-6 6"></path>
          <path d="m9 9 6 6"></path>
        </g>
        
        <g *ngSwitchCase="'info'">
          <circle cx="12" cy="12" r="10"></circle>
          <path d="M12 16v-4"></path>
          <path d="M12 8h.01"></path>
        </g>
        
        <!-- ============================================
             ACCIONES
             ============================================ -->
        
        <g *ngSwitchCase="'zap'">
          <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
        </g>
        
        <g *ngSwitchCase="'refresh-cw'">
          <path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8"></path>
          <path d="M21 3v5h-5"></path>
          <path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16"></path>
          <path d="M3 21v-5h5"></path>
        </g>
        
        <g *ngSwitchCase="'search'">
          <circle cx="11" cy="11" r="8"></circle>
          <path d="m21 21-4.3-4.3"></path>
        </g>
        
        <g *ngSwitchCase="'eye'">
          <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"></path>
          <circle cx="12" cy="12" r="3"></circle>
        </g>
        
        <g *ngSwitchCase="'printer'">
          <polyline points="6 9 6 2 18 2 18 9"></polyline>
          <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
          <rect width="12" height="8" x="6" y="14"></rect>
        </g>

        <!--  Filtros y Exportación -->
        <g *ngSwitchCase="'filter'">
          <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"></polygon>
        </g>

        <g *ngSwitchCase="'download'">
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
          <polyline points="7 10 12 15 17 10"></polyline>
          <line x1="12" x2="12" y1="15" y2="3"></line>
        </g>

        <!-- Upload y File Badge -->
        <g *ngSwitchCase="'upload'">
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
          <polyline points="17 8 12 3 7 8"></polyline>
          <line x1="12" x2="12" y1="3" y2="15"></line>
        </g>

        <g *ngSwitchCase="'file-badge'">
          <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"></path>
          <polyline points="14 2 14 8 20 8"></polyline>
          <path d="M12 11.5a2.5 2.5 0 1 0 0 5 2.5 2.5 0 0 0 0-5Z"></path>
          <path d="M9 16.5v1.5"></path>
          <path d="M15 16.5v1.5"></path>
        </g>

        <g *ngSwitchCase="'loader'">
          <line x1="12" x2="12" y1="2" y2="6"></line>
          <line x1="12" x2="12" y1="18" y2="22"></line>
          <line x1="4.93" x2="7.76" y1="4.93" y2="7.76"></line>
          <line x1="16.24" x2="19.07" y1="16.24" y2="19.07"></line>
          <line x1="2" x2="6" y1="12" y2="12"></line>
          <line x1="18" x2="22" y1="12" y2="12"></line>
          <line x1="4.93" x2="7.76" y1="19.07" y2="16.24"></line>
          <line x1="16.24" x2="19.07" y1="7.76" y2="4.93"></line>
        </g>

        <!--  Operaciones CRUD -->
        <g *ngSwitchCase="'plus'">
          <path d="M5 12h14"></path>
          <path d="M12 5v14"></path>
        </g>

        <g *ngSwitchCase="'minus'">
          <path d="M5 12h14"></path>
        </g>

        <g *ngSwitchCase="'edit'">
          <path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z"></path>
          <path d="m15 5 4 4"></path>
        </g>

        <g *ngSwitchCase="'trash'">
          <path d="M3 6h18"></path>
          <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"></path>
          <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"></path>
        </g>
        
        <!-- ============================================
             TIEMPO Y CALENDARIO
             ============================================ -->
        
        <g *ngSwitchCase="'clock'">
          <circle cx="12" cy="12" r="10"></circle>
          <polyline points="12 6 12 12 16 14"></polyline>
        </g>
        
        <g *ngSwitchCase="'calendar'">
          <rect width="18" height="18" x="3" y="4" rx="2" ry="2"></rect>
          <line x1="16" x2="16" y1="2" y2="6"></line>
          <line x1="8" x2="8" y1="2" y2="6"></line>
          <line x1="3" x2="21" y1="10" y2="10"></line>
        </g>
        
        <!-- ============================================
             USUARIOS Y ROLES
             ============================================ -->
        
        <g *ngSwitchCase="'user'">
          <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"></path>
          <circle cx="12" cy="7" r="4"></circle>
        </g>
        
        <g *ngSwitchCase="'users'">
          <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"></path>
          <circle cx="9" cy="7" r="4"></circle>
          <path d="M22 21v-2a4 4 0 0 0-3-3.87"></path>
          <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
        </g>
        
        <!-- ============================================
             IDENTIFICADORES Y NÚMEROS
             ============================================ -->
        
        <g *ngSwitchCase="'hash'">
          <line x1="4" x2="20" y1="9" y2="9"></line>
          <line x1="4" x2="20" y1="15" y2="15"></line>
          <line x1="10" x2="8" y1="3" y2="21"></line>
          <line x1="16" x2="14" y1="3" y2="21"></line>
        </g>
        
        <g *ngSwitchCase="'qr-code'">
          <rect width="5" height="5" x="3" y="3" rx="1"></rect>
          <rect width="5" height="5" x="16" y="3" rx="1"></rect>
          <rect width="5" height="5" x="3" y="16" rx="1"></rect>
          <path d="M21 16h-3a2 2 0 0 0-2 2v3"></path>
          <path d="M21 21v.01"></path>
          <path d="M12 7v3a2 2 0 0 1-2 2H7"></path>
          <path d="M3 12h.01"></path>
          <path d="M12 3h.01"></path>
          <path d="M12 16v.01"></path>
          <path d="M16 12h1"></path>
          <path d="M21 12v.01"></path>
          <path d="M12 21v-1"></path>
        </g>
        
        <!-- ============================================
             FINANCIERO
             ============================================ -->
        
        <g *ngSwitchCase="'credit-card'">
          <rect width="20" height="14" x="2" y="5" rx="2"></rect>
          <line x1="2" x2="22" y1="10" y2="10"></line>
        </g>
        
        <!-- ============================================
             SALUD Y HOSPITAL
             ============================================ -->
        
        <g *ngSwitchCase="'droplet'">
          <path d="M12 2.69l5.66 5.66a8 8 0 1 1-11.31 0z"></path>
        </g>
        
        <g *ngSwitchCase="'building-2'">
          <path d="M6 22V4a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v18Z"></path>
          <path d="M6 12H4a2 2 0 0 0-2 2v6a2 2 0 0 0 2 2h2"></path>
          <path d="M18 9h2a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2h-2"></path>
          <path d="M10 6h4"></path>
          <path d="M10 10h4"></path>
          <path d="M10 14h4"></path>
          <path d="M10 18h4"></path>
        </g>
        
        <!-- ============================================
             TRANSPORTE Y UBICACIÓN
             ============================================ -->
        
        <g *ngSwitchCase="'truck'">
          <path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"></path>
          <path d="M15 18H9"></path>
          <path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"></path>
          <circle cx="17" cy="18" r="2"></circle>
          <circle cx="7" cy="18" r="2"></circle>
        </g>

        <g *ngSwitchCase="'map-pin'">
          <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"></path>
          <circle cx="12" cy="10" r="3"></circle>
        </g>

        <!-- ============================================
             CONFIGURACIÓN
             ============================================ -->

        <!-- ⭐ NUEVO: Settings (Mantenimiento) -->
        <g *ngSwitchCase="'settings'">
          <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"></path>
          <circle cx="12" cy="12" r="3"></circle>
        </g>

        <!-- ⭐ NUEVO: Ban (Fuera de Servicio) -->
        <g *ngSwitchCase="'ban'">
          <circle cx="12" cy="12" r="10"></circle>
          <path d="m4.9 4.9 14.2 14.2"></path>
        </g>
        
        <!-- ============================================
             ADMINISTRATIVO
             ============================================ -->
        
        <g *ngSwitchCase="'bell'">
          <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"></path>
          <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0"></path>
        </g>

        <g *ngSwitchCase="'book'">
          <path d="M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H20v20H6.5a2.5 2.5 0 0 1 0-5H20"></path>
        </g>

        <g *ngSwitchCase="'shield'">
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10"></path>
        </g>

        <g *ngSwitchCase="'bar-chart'">
          <line x1="12" x2="12" y1="20" y2="10"></line>
          <line x1="18" x2="18" y1="20" y2="4"></line>
          <line x1="6" x2="6" y1="20" y2="16"></line>
        </g>
        
        <!-- ============================================
             DEFAULT (FALLBACK)
             ============================================ -->
        
        <g *ngSwitchDefault>
          <circle cx="12" cy="12" r="10"></circle>
          <path d="M12 16v-4"></path>
          <path d="M12 8h.01"></path>
        </g>
        
      </ng-container>
    </svg>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      line-height: 0;
    }
  `]
})
export class IconComponent {
  @Input() name: string = '';
  @Input() size: number = 24;
  @Input() className: string = '';
}
