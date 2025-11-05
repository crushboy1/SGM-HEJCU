namespace SisMortuorio.Data.Entities.Enums
{
    /// <summary>
    /// Eventos (triggers) que causan transiciones de estado en el expediente
    /// </summary>
    public enum TriggerExpediente
    {
        /// <summary>
        /// Enfermería genera el QR del expediente
        /// Transición: EnPiso → PendienteDeRecojo
        /// </summary>
        GenerarQR = 1,

        /// <summary>
        /// Ambulancia acepta la custodia del cuerpo
        /// Transición: PendienteDeRecojo → EnTrasladoMortuorio
        /// </summary>
        AceptarCustodia = 2,

        /// <summary>
        /// Vigilante verifica y aprueba el ingreso al mortuorio
        /// Transición: EnTrasladoMortuorio → EnMortuorio
        /// </summary>
        VerificarIngresoMortuorio = 3,

        /// <summary>
        /// Vigilante rechaza la verificación por datos incorrectos
        /// Transición: EnTrasladoMortuorio → VerificacionRechazadaMortuorio
        /// </summary>
        RechazarVerificacion = 4,

        /// <summary>
        /// Enfermería corrige los datos y se reimprime brazalete
        /// Transición: VerificacionRechazadaMortuorio → EnTrasladoMortuorio
        /// </summary>
        CorregirDatos = 5,

        /// <summary>
        /// Técnico asigna bandeja en el mortuorio
        /// Nota: NO cambia estado, solo actualiza ubicación
        /// </summary>
        AsignarBandeja = 6,

        /// <summary>
        /// Familiar es autorizado y se marca como pendiente de retiro
        /// Transición: EnBandeja → PendienteRetiro
        /// </summary>
        AutorizarRetiro = 7,

        /// <summary>
        /// Vigilante registra la salida física del cuerpo
        /// Transición: PendienteRetiro → Retirado
        /// </summary>
        RegistrarSalida = 8
    }
}