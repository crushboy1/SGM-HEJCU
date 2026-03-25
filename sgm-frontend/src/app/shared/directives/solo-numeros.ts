import { Directive, HostListener, Input } from '@angular/core';

/**
 * Directiva para campos numéricos — bloquea teclas no numéricas.
 * Uso simple (siempre activa):   <input appSoloNumeros>
 * Uso condicional (booleano):    <input [appSoloNumeros]="esDni('tipoDoc')">
 *
 * Permite: 0-9, Backspace, Delete, Tab, flechas, Home/End, Ctrl/Cmd (copiar/pegar).
 */
@Directive({
  selector: '[appSoloNumeros]',
  standalone: true
})
export class SoloNumerosDirective {

  /**
   * Controla si la directiva está activa.
   * - Sin binding: el atributo solo se evalúa como presente → activa (string vacío → truthy check en setter)
   * - Con binding booleano [appSoloNumeros]="true/false" → respeta el valor
   */
  @Input() set appSoloNumeros(valor: boolean | string | '') {
    // Si no se pasa valor (atributo solo) Angular pasa string vacío ''
    // Si se pasa booleano, respeta el valor
    this.activa = valor === '' || valor === true;
  }

  private activa = true;

  private readonly allowedKeys = [
    'Backspace', 'Delete', 'Tab',
    'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
    'Home', 'End', 'Enter'
  ];

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (!this.activa) return;

    // Permite Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X (copiar/pegar)
    if (event.ctrlKey || event.metaKey) return;

    // Permite teclas de navegación y borrado
    if (this.allowedKeys.includes(event.key)) return;

    // Bloquea si no es dígito 0-9
    if (!/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }
}
