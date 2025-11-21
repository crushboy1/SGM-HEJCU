import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

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
        <g *ngSwitchCase="'home'">
          <path d="m3 9 9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
          <polyline points="9 22 9 12 15 12 15 22"></polyline>
        </g>
        
        <g *ngSwitchCase="'inbox'">
          <polyline points="22 12 16 12 14 15 10 15 8 12 2 12"></polyline>
          <path d="M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"></path>
        </g>
        
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
        
        <g *ngSwitchCase="'chevron-left'">
          <path d="m15 18-6-6 6-6"></path>
        </g>
        
        <g *ngSwitchCase="'chevron-right'">
          <path d="m9 18 6-6-6-6"></path>
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
        
        <g *ngSwitchCase="'clock'">
          <circle cx="12" cy="12" r="10"></circle>
          <polyline points="12 6 12 12 16 14"></polyline>
        </g>
        
        <g *ngSwitchCase="'archive'">
          <rect width="20" height="5" x="2" y="3" rx="1"></rect>
          <path d="M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8"></path>
          <path d="M10 12h4"></path>
        </g>
        
        <g *ngSwitchCase="'alert-triangle'">
          <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"></path>
          <path d="M12 9v4"></path>
          <path d="M12 17h.01"></path>
        </g>
        
        <g *ngSwitchCase="'refresh-cw'">
          <path d="M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8"></path>
          <path d="M21 3v5h-5"></path>
          <path d="M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16"></path>
          <path d="M3 21v-5h5"></path>
        </g>
        
        <g *ngSwitchCase="'eye'">
          <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"></path>
          <circle cx="12" cy="12" r="3"></circle>
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
        
        <g *ngSwitchCase="'zap'">
          <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon>
        </g>
        
        <g *ngSwitchCase="'clipboard-list'">
          <rect width="8" height="4" x="8" y="2" rx="1" ry="1"></rect>
          <path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"></path>
          <path d="M12 11h4"></path>
          <path d="M12 16h4"></path>
          <path d="M8 11h.01"></path>
          <path d="M8 16h.01"></path>
        </g>
        
        <g *ngSwitchCase="'file-check'">
          <path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"></path>
          <polyline points="14 2 14 8 20 8"></polyline>
          <path d="m9 15 2 2 4-4"></path>
        </g>
        
        <g *ngSwitchCase="'truck'">
          <path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2"></path>
          <path d="M15 18H9"></path>
          <path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14"></path>
          <circle cx="17" cy="18" r="2"></circle>
          <circle cx="7" cy="18" r="2"></circle>
        </g>

        <g *ngSwitchCase="'printer'">
          <polyline points="6 9 6 2 18 2 18 9"></polyline>
          <path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"></path>
          <rect width="12" height="8" x="6" y="14"></rect>
        </g>

        <g *ngSwitchCase="'search'">
          <circle cx="11" cy="11" r="8"></circle>
          <path d="m21 21-4.3-4.3"></path>
        </g>

        <g *ngSwitchCase="'map-pin'">
          <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"></path>
          <circle cx="12" cy="10" r="3"></circle>
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
        
        <g *ngSwitchDefault>
          <circle cx="12" cy="12" r="10"></circle>
          <path d="M12 16v-4"></path>
          <path d="M12 8h.01"></path>
        </g>

        <g *ngSwitchCase="'chevron-up'">
        <path d="m18 15-6-6-6 6"></path>
        </g>

        <g *ngSwitchCase="'chevron-down'">
        <path d="m6 9 6 6 6-6"></path>
        </g>

        <g *ngSwitchCase="'bell'">
        <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9"></path>
        <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0"></path>
        </g>

        <g *ngSwitchCase="'users'">
        <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"></path>
        <circle cx="9" cy="7" r="4"></circle>
        <path d="M22 21v-2a4 4 0 0 0-3-3.87"></path>
        <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
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

        <g *ngSwitchCase="'info'">
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
}
