import { Directive, HostListener, Input, ElementRef, OnChanges } from '@angular/core';

/**
 * Directiva para campos numéricos — bloquea teclas no numéricas.
 * Uso simple (siempre activa):   <input appSoloNumeros>
 * Uso condicional (booleano):    <input [appSoloNumeros]="esDni">
 *
 * Permite: 0-9, Backspace, Delete, Tab, flechas, Home/End, Ctrl/Cmd (copiar/pegar).
 * Cubre: keydown, paste, drag&drop, autocompletado, mobile keyboards.
 * Fix: detecta cambio OFF→ON y limpia el input automáticamente.
 */
@Directive({
  selector: '[appSoloNumeros]',
  standalone: true
})
export class SoloNumerosDirective implements OnChanges {

  @Input() appSoloNumeros: boolean | string | '' = '';

  private activa = true;

  private readonly allowedKeys = [
    'Backspace', 'Delete', 'Tab',
    'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
    'Home', 'End', 'Enter'
  ];

  constructor(private el: ElementRef<HTMLInputElement>) { }

  ngOnChanges(): void {
    this.activa = this.appSoloNumeros === '' || this.appSoloNumeros === true;
    if (this.activa) {
      setTimeout(() => this.limpiarInput());
    }
  }

  @HostListener('blur')
  onBlur(): void {
    if (!this.activa) return;
    this.limpiarInput();
  }

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (!this.activa) return;
    if (event.ctrlKey || event.metaKey) return;
    if (this.allowedKeys.includes(event.key)) return;
    if (!/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }

  @HostListener('input')
  onInput(): void {
    if (!this.activa) return;
    const input = this.el.nativeElement;
    const maxlength = input.maxLength > 0 ? input.maxLength : undefined;
    let limpio = input.value.replace(/\D/g, '');
    if (maxlength) limpio = limpio.slice(0, maxlength);
    if (input.value !== limpio) {
      input.value = limpio;
      input.dispatchEvent(new Event('input', { bubbles: true }));
    }
  }

  private limpiarInput(): void {
    const input = this.el.nativeElement;
    const maxlength = input.maxLength > 0 ? input.maxLength : undefined;
    let limpio = input.value.replace(/\D/g, '');
    if (maxlength) limpio = limpio.slice(0, maxlength);
    if (input.value !== limpio) {
      input.value = limpio;
      input.dispatchEvent(new Event('input', { bubbles: true }));
    }
  }
}
