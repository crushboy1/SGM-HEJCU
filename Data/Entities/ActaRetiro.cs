using System;
using System.ComponentModel.DataAnnotations;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data.Entities;

/// <summary>
/// Acta de Retiro de Cadáver - Documento oficial tripartito
/// Generado por Admisión para casos de fallecimiento interno (> 24/48h EN hospital)
/// Requiere firmas de: Admisionista, Familiar y Supervisor de Vigilancia
/// Se genera PDF imprimible, se firma físicamente y se escanea para archivo digital
/// </summary>
public class ActaRetiro
{
    /// <summary>
    /// Identificador único del acta
    /// </summary>
    [Key]
    public int ActaRetiroID { get; set; }

    // ===================================================================
    // RELACIÓN CON EXPEDIENTE
    // ===================================================================

    /// <summary>
    /// ID del expediente asociado (relación 1:1)
    /// </summary>
    [Required]
    public int ExpedienteID { get; set; }

    /// <summary>
    /// Navegación al expediente
    /// </summary>
    public virtual Expediente Expediente { get; set; } = null!;


    // ===================================================================
    // DATOS DEL FALLECIDO (DENORMALIZADOS PARA PDF)
    // ===================================================================

    /// <summary>
    /// Número de Certificado de Defunción SINADEF
    /// OBLIGATORIO si TipoSalida = Familiar
    /// Formato: 12 dígitos numéricos
    /// </summary>
    [MaxLength(50)]
    public string? NumeroCertificadoDefuncion { get; set; }

    /// <summary>
    /// Número de Oficio Legal (PNP/Fiscalía)
    /// OBLIGATORIO si TipoSalida = AutoridadLegal
    /// Reemplaza al Certificado SINADEF en casos externos
    /// Ej: "OFICIO N° 1262-2025-REG.POL-LIMA/DIVTER-COMISARIA-SA"
    /// </summary>
    [MaxLength(150)]
    public string? NumeroOficioLegal { get; set; }

    /// <summary>
    /// Nombre completo del fallecido (denormalizado para PDF)
    /// Se genera automáticamente desde Expediente
    /// Formato: "Apellido Paterno Apellido Materno, Nombres"
    /// </summary>
    [MaxLength(300)]
    public string? NombreCompletoFallecido { get; set; }

    /// <summary>
    /// Historia Clínica del fallecido
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string HistoriaClinica { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de documento del fallecido (DNI, CE, Pasaporte, NN)
    /// </summary>
    [Required]
    public TipoDocumentoIdentidad TipoDocumentoFallecido { get; set; }

    /// <summary>
    /// Número de documento del fallecido
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string NumeroDocumentoFallecido { get; set; } = string.Empty;

    /// <summary>
    /// Servicio hospitalario donde ocurrió el fallecimiento
    /// Ej: UCI, Emergencia, Medicina Interna, Cirugía
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ServicioFallecimiento { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora del fallecimiento
    /// </summary>
    [Required]
    public DateTime FechaHoraFallecimiento { get; set; }

    // ===================================================================
    // MÉDICO CERTIFICANTE
    // ===================================================================

    /// <summary>
    /// Nombre completo del médico que certifica el fallecimiento
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string MedicoCertificaNombre { get; set; } = string.Empty;

    /// <summary>
    /// CMP del médico certificante
    /// Colegio Médico del Perú - 5 dígitos
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string MedicoCMP { get; set; } = string.Empty;

    /// <summary>
    /// RNE del médico (opcional)
    /// Registro Nacional de Especialidades - 5 dígitos
    /// Solo si el médico tiene especialidad registrada
    /// </summary>
    [MaxLength(20)]
    public string? MedicoRNE { get; set; }

    // ===================================================================
    // JEFE DE GUARDIA
    // ===================================================================

    /// <summary>
    /// Nombre completo del Jefe de Guardia del turno
    /// </summary>
    [Required]
    [MaxLength(300)]
    public string JefeGuardiaNombre { get; set; } = string.Empty;

    /// <summary>
    /// CMP del Jefe de Guardia
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string JefeGuardiaCMP { get; set; } = string.Empty;

    // ===================================================================
    // TIPO DE SALIDA Y RESPONSABLE
    // ===================================================================

    /// <summary>
    /// Tipo de salida del mortuorio
    /// - Familiar: Retiro por familiar directo (> 24h en hospital)
    /// - AutoridadLegal: Retiro por PNP/Fiscal/Legista (< 24h o muerte violenta)
    /// </summary>
    [Required]
    public TipoSalida TipoSalida { get; set; } = TipoSalida.Familiar;

    // ===================================================================
    // RESPONSABLE - FAMILIAR
    // ===================================================================

    /// <summary>
    /// Apellido paterno del familiar
    /// </summary>
    [MaxLength(100)]
    public string? FamiliarApellidoPaterno { get; set; }

    /// <summary>
    /// Apellido materno del familiar
    /// </summary>
    [MaxLength(100)]
    public string? FamiliarApellidoMaterno { get; set; }

    /// <summary>
    /// Nombres del familiar
    /// </summary>
    [MaxLength(100)]
    public string? FamiliarNombres { get; set; }

    /// <summary>
    /// Nombre completo del familiar (denormalizado para reportes)
    /// Se genera automáticamente: "ApellidoPaterno ApellidoMaterno, Nombres"
    /// </summary>
    [MaxLength(300)]
    public string? FamiliarNombreCompleto { get; set; }

    /// <summary>
    /// Tipo de documento del familiar
    /// </summary>
    public TipoDocumentoIdentidad? FamiliarTipoDocumento { get; set; }

    /// <summary>
    /// Número de documento del familiar
    /// </summary>
    [MaxLength(50)]
    public string? FamiliarNumeroDocumento { get; set; }

    /// <summary>
    /// Parentesco del familiar con el fallecido
    /// Valores típicos: Hijo/a, Padre, Madre, Cónyuge, Hermano/a
    /// </summary>
    [MaxLength(100)]
    public string? FamiliarParentesco { get; set; }

    /// <summary>
    /// Teléfono de contacto del familiar
    /// </summary>
    [MaxLength(20)]
    public string? FamiliarTelefono { get; set; }
    // ===================================================================
    // RESPONSABLE - AUTORIDAD LEGAL
    // ===================================================================

    /// <summary>
    /// Apellido paterno de la autoridad
    /// </summary>
    [MaxLength(100)]
    public string? AutoridadApellidoPaterno { get; set; }

    /// <summary>
    /// Apellido materno de la autoridad
    /// </summary>
    [MaxLength(100)]
    public string? AutoridadApellidoMaterno { get; set; }

    /// <summary>
    /// Nombres de la autoridad
    /// </summary>
    [MaxLength(100)]
    public string? AutoridadNombres { get; set; }

    /// <summary>
    /// Nombre completo de la autoridad (denormalizado)
    /// Se genera automáticamente: "ApellidoPaterno ApellidoMaterno, Nombres"
    /// </summary>
    [MaxLength(300)]
    public string? AutoridadNombreCompleto { get; set; }

    /// <summary>
    /// Tipo de autoridad que retira el cuerpo
    /// OBLIGATORIO si TipoSalida = AutoridadLegal
    /// Valores: Policia, Fiscal, MedicoLegista
    /// </summary>
    public TipoAutoridadExterna? TipoAutoridad { get; set; }

    /// <summary>
    /// Tipo de documento de la autoridad
    /// Típicamente DNI o Carné Institucional
    /// </summary>
    public TipoDocumentoIdentidad? AutoridadTipoDocumento { get; set; }

    /// <summary>
    /// Número de documento de la autoridad
    /// </summary>
    [MaxLength(50)]
    public string? AutoridadNumeroDocumento { get; set; }

    /// <summary>
    /// Cargo de la autoridad
    /// Ej: "Suboficial PNP", "Fiscal Provincial", "Médico Legista II"
    /// </summary>
    [MaxLength(100)]
    public string? AutoridadCargo { get; set; }

    /// <summary>
    /// Institución de la autoridad
    /// Ej: "Comisaría San Antonio", "Fiscalía de Turno Miraflores", "Morgue Central Lima"
    /// </summary>
    [MaxLength(200)]
    public string? AutoridadInstitucion { get; set; }

    /// <summary>
    /// Placa del vehículo oficial/patrullero
    /// Ej: "PNP-1234", "A1B-987"
    /// </summary>
    [MaxLength(20)]
    public string? AutoridadPlacaVehiculo { get; set; }

    /// <summary>
    /// Teléfono de contacto de la autoridad o institución
    /// </summary>
    [MaxLength(20)]
    public string? AutoridadTelefono { get; set; }

    // ===================================================================
    // DATOS ADICIONALES (TRASLADO/OTRO)
    // ===================================================================

    /// <summary>
    /// Datos adicionales para caso TrasladoHospital u Otro
    /// Ej: Hospital destino, Comisaría, Morgue Central
    /// </summary>
    [MaxLength(500)]
    public string? DatosAdicionales { get; set; }

    /// <summary>
    /// Destino final del cuerpo
    /// Ej: Cementerio El Ángel, Crematorio Municipal, Morgue Central
    /// </summary>
    [MaxLength(200)]
    public string? Destino { get; set; }

    // ===================================================================
    // FIRMAS 
    // ===================================================================

    /// <summary>
    /// Indica si el acta fue firmada por el responsable del retiro
    /// - Si TipoSalida = Familiar → Firma el familiar
    /// - Si TipoSalida = AutoridadLegal → Firma la autoridad (PNP/Fiscal/Legista)
    /// Se marca true cuando se sube el PDF escaneado con firmas
    /// </summary>
    public bool FirmadoResponsable { get; set; } = false;

    /// <summary>
    /// Fecha y hora en que firmó el responsable
    /// </summary>
    public DateTime? FechaFirmaResponsable { get; set; }

    /// <summary>
    /// Indica si el acta fue firmada por el admisionista
    /// </summary>
    public bool FirmadoAdmisionista { get; set; } = false;

    /// <summary>
    /// Fecha y hora en que firmó el admisionista
    /// </summary>
    public DateTime? FechaFirmaAdmisionista { get; set; }

    /// <summary>
    /// Indica si el acta fue firmada y sellada por el Supervisor de Vigilancia
    /// </summary>
    public bool FirmadoSupervisorVigilancia { get; set; } = false;

    /// <summary>
    /// Fecha y hora en que firmó el Supervisor de Vigilancia
    /// </summary>
    public DateTime? FechaSupervisorVigilancia { get; set; }

    // ===================================================================
    // ARCHIVOS PDF
    // ===================================================================

    /// <summary>
    /// Ruta del PDF generado inicialmente (sin firmas)
    /// Formato: "uploads/actas-retiro/2025/01/SGM-2025-00152-acta-sin-firmas.pdf"
    /// </summary>
    [MaxLength(500)]
    public string? RutaPDFSinFirmar { get; set; }

    /// <summary>
    /// Nombre del archivo PDF sin firmar
    /// </summary>
    [MaxLength(255)]
    public string? NombreArchivoPDFSinFirmar { get; set; }

    /// <summary>
    /// Tamaño del archivo PDF sin firmar (bytes)
    /// </summary>
    public long? TamañoPDFSinFirmar { get; set; }

    /// <summary>
    /// Ruta del PDF escaneado con las 3 firmas físicas
    /// Formato: "uploads/actas-retiro/2025/01/SGM-2025-00152-acta-firmada.pdf"
    /// </summary>
    [MaxLength(500)]
    public string? RutaPDFFirmado { get; set; }

    /// <summary>
    /// Nombre del archivo PDF firmado
    /// </summary>
    [MaxLength(255)]
    public string? NombreArchivoPDFFirmado { get; set; }

    /// <summary>
    /// Tamaño del archivo PDF firmado (bytes)
    /// </summary>
    public long? TamañoPDFFirmado { get; set; }

    // ===================================================================
    // OBSERVACIONES
    // ===================================================================

    /// <summary>
    /// Observaciones generales sobre el acta o el retiro
    /// Ej: "Familiar presentó copia de DNI", "Se entregó un juego completo de documentos"
    /// </summary>
    [MaxLength(1000)]
    public string? Observaciones { get; set; }

    // ===================================================================
    // AUDITORÍA
    // ===================================================================

    /// <summary>
    /// Usuario de Admisión que generó el acta
    /// </summary>
    [Required]
    public int UsuarioAdmisionID { get; set; }

    /// <summary>
    /// Navegación al usuario de Admisión
    /// </summary>
    public virtual Usuario UsuarioAdmision { get; set; } = null!;

    /// <summary>
    /// Fecha y hora en que se generó el acta
    /// </summary>
    [Required]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    /// <summary>
    /// Usuario que subió el PDF firmado (puede ser Admisionista o Sup. Vigilancia)
    /// </summary>
    public int? UsuarioSubidaPDFID { get; set; }

    /// <summary>
    /// Navegación al usuario que subió el PDF firmado
    /// </summary>
    public virtual Usuario? UsuarioSubidaPDF { get; set; }

    /// <summary>
    /// Fecha y hora en que se subió el PDF firmado
    /// </summary>
    public DateTime? FechaSubidaPDF { get; set; }

    // ===================================================================
    // RELACIÓN CON SALIDA MORTUORIO
    // ===================================================================

    /// <summary>
    /// Navegación a la salida física registrada por Vigilante Mortuorio
    /// Relación inversa 1:1
    /// </summary>
    public virtual SalidaMortuorio? SalidaMortuorio { get; set; }

    // ===================================================================
    // MÉTODOS DE LÓGICA DE NEGOCIO
    // ===================================================================

    /// <summary>
    /// Verifica si el acta está completa (todos los campos obligatorios llenos)
    /// </summary>
    /// <summary>
    /// Verifica si el acta está completa según el tipo de salida
    /// </summary>
    public bool EstaCompleta()
    {
        // Datos básicos (siempre obligatorios)
        bool datosBasicos = !string.IsNullOrWhiteSpace(MedicoCertificaNombre) &&
                            !string.IsNullOrWhiteSpace(JefeGuardiaNombre);

        if (!datosBasicos) return false;

        // Validar según tipo de salida
        if (TipoSalida == TipoSalida.Familiar)
        {
            return !string.IsNullOrWhiteSpace(NumeroCertificadoDefuncion) &&
                   !string.IsNullOrWhiteSpace(FamiliarApellidoPaterno) &&
                   !string.IsNullOrWhiteSpace(FamiliarNombres) &&
                   !string.IsNullOrWhiteSpace(FamiliarNumeroDocumento) &&
                   !string.IsNullOrWhiteSpace(FamiliarParentesco);
        }
        else if (TipoSalida == TipoSalida.AutoridadLegal)
        {
            return !string.IsNullOrWhiteSpace(NumeroOficioLegal) &&
                   !string.IsNullOrWhiteSpace(AutoridadApellidoPaterno) &&
                   !string.IsNullOrWhiteSpace(AutoridadNombres) &&
                   !string.IsNullOrWhiteSpace(AutoridadNumeroDocumento) &&
                   !string.IsNullOrWhiteSpace(AutoridadInstitucion) &&
                   TipoAutoridad != null;
        }

        return false;
    }

    /// <summary>
    /// Genera el nombre completo del familiar
    /// Debe llamarse antes de guardar en BD
    /// </summary>
    public void GenerarNombreCompletoFamiliar()
    {
        if (!string.IsNullOrWhiteSpace(FamiliarApellidoPaterno) &&
            !string.IsNullOrWhiteSpace(FamiliarNombres))
        {
            FamiliarNombreCompleto = $"{FamiliarApellidoPaterno} {FamiliarApellidoMaterno ?? ""}, {FamiliarNombres}".Trim();
        }
    }

    /// <summary>
    /// Genera el nombre completo de la autoridad
    /// Debe llamarse antes de guardar en BD
    /// </summary>
    public void GenerarNombreCompletoAutoridad()
    {
        if (!string.IsNullOrWhiteSpace(AutoridadApellidoPaterno) &&
            !string.IsNullOrWhiteSpace(AutoridadNombres))
        {
            AutoridadNombreCompleto = $"{AutoridadApellidoPaterno} {AutoridadApellidoMaterno ?? ""}, {AutoridadNombres}".Trim();
        }
    }

    /// <summary>
    /// Verifica si el acta tiene todas las firmas requeridas 
    /// </summary>
    public bool TieneTodasLasFirmas()
    {
        return FirmadoResponsable && FirmadoAdmisionista && FirmadoSupervisorVigilancia;
    }

    /// <summary>
    /// Verifica si el PDF firmado fue subido al sistema
    /// </summary>
    public bool TienePDFFirmado()
    {
        return !string.IsNullOrWhiteSpace(RutaPDFFirmado) && TamañoPDFFirmado > 0;
    }

    /// <summary>
    /// Marca el acta como firmada por las 3 partes
    /// Se ejecuta al subir el PDF escaneado con todas las firmas
    /// </summary>
    /// <param name="usuarioSubidaID">Usuario que subió el PDF firmado</param>
    public void MarcarFirmadoCompleto(int usuarioSubidaID)
    {
        // Firma del responsable (Familiar o Autoridad)
        FirmadoResponsable = true;
        FechaFirmaResponsable = DateTime.Now;

        FirmadoAdmisionista = true;
        FechaFirmaAdmisionista = DateTime.Now;

        FirmadoSupervisorVigilancia = true;
        FechaSupervisorVigilancia = DateTime.Now;

        // Metadata de subida
        UsuarioSubidaPDFID = usuarioSubidaID;
        FechaSubidaPDF = DateTime.Now;
    }

    /// <summary>
    /// Obtiene el nombre del responsable que debe firmar según el tipo de salida
    /// Útil para mostrar en UI: "Firma del Familiar" vs "Firma de la Autoridad"
    /// </summary>
    public string ObtenerNombreResponsableFirma()
    {
        if (TipoSalida == TipoSalida.Familiar)
        {
            return FamiliarNombreCompleto ?? "Familiar Responsable";
        }
        else if (TipoSalida == TipoSalida.AutoridadLegal)
        {
            var tipoAuth = TipoAutoridad switch
            {
                TipoAutoridadExterna.Policia => "PNP",
                TipoAutoridadExterna.Fiscal => "Fiscal",
                TipoAutoridadExterna.MedicoLegista => "Médico Legista",
                _ => "Autoridad"
            };
            return $"{AutoridadNombreCompleto ?? tipoAuth}";
        }

        return "Responsable del Retiro";
    }
    /// <summary>
    /// Obtiene el tamaño del PDF sin firmar en formato legible
    /// </summary>
    public string? ObtenerTamañoPDFSinFirmarLegible()
    {
        if (TamañoPDFSinFirmar is null) return null;

        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = TamañoPDFSinFirmar.Value;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Obtiene el tamaño del PDF firmado en formato legible
    /// </summary>
    public string? ObtenerTamañoPDFFirmadoLegible()
    {
        if (TamañoPDFFirmado is null) return null;

        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = TamañoPDFFirmado.Value;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Valida que el acta esté lista para generar el PDF
    /// </summary>
    /// <summary>
    /// Valida que el acta esté lista para generar el PDF
    /// </summary>
    public string ValidarParaGenerarPDF()
    {
        if (!EstaCompleta())
            return "El acta no está completa. Faltan datos obligatorios.";

        // Validar según tipo de salida
        if (TipoSalida == TipoSalida.Familiar)
        {
            if (string.IsNullOrWhiteSpace(NumeroCertificadoDefuncion))
                return "Falta el número de certificado SINADEF (obligatorio para retiros por familiar).";

            if (string.IsNullOrWhiteSpace(FamiliarParentesco))
                return "Debe especificar el parentesco del familiar.";
        }
        else if (TipoSalida == TipoSalida.AutoridadLegal)
        {
            if (string.IsNullOrWhiteSpace(NumeroOficioLegal))
                return "Falta el número de oficio legal (obligatorio para retiros por autoridades).";

            if (string.IsNullOrWhiteSpace(AutoridadInstitucion))
                return "Debe especificar la institución de la autoridad.";

            if (TipoAutoridad == null)
                return "Debe especificar el tipo de autoridad (PNP, Fiscal, Médico Legista).";
        }

        return "OK";
    }
}