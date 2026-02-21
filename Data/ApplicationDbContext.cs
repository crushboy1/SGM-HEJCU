using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.Entities.Enums;

namespace SisMortuorio.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario, Rol, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ═══════════════════════════════════════════════════════════
        // DBSETS
        // ═══════════════════════════════════════════════════════════

        public DbSet<Expediente> Expedientes { get; set; }
        public DbSet<Pertenencia> Pertenencias { get; set; }
        public DbSet<CustodiaTransferencia> CustodiaTransferencias { get; set; }

        // Fase 4-5: Módulos Administrativos
        public DbSet<DeudaEconomica> DeudasEconomicas { get; set; }
        public DbSet<DeudaSangre> DeudasSangre { get; set; }
        public DbSet<Bandeja> Bandejas { get; set; }
        public DbSet<BandejaHistorial> BandejaHistoriales { get; set; }
        public DbSet<VerificacionMortuorio> VerificacionesMortuorio { get; set; }
        public DbSet<SalidaMortuorio> SalidasMortuorio { get; set; }
        public DbSet<ActaRetiro> ActasRetiro { get; set; } = null!;
        public DbSet<SolicitudCorreccionExpediente> SolicitudesCorreccion { get; set; }

        // Casos Externos
        /// <summary>
        /// Expedientes legales (casos externos con intervención policial/fiscal).
        /// </summary>
        public DbSet<ExpedienteLegal> ExpedientesLegales { get; set; } = null!;

        /// <summary>
        /// Autoridades externas (policía, fiscal, médico legista).
        /// </summary>
        public DbSet<AutoridadExterna> AutoridadesExternas { get; set; } = null!;

        /// <summary>
        /// Documentos legales escaneados (Epicrisis, Oficios, Actas).
        /// </summary>
        public DbSet<DocumentoLegal> DocumentosLegales { get; set; } = null!;
        // Auditoría
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Deshabilitar cascadas en Identity para evitar ciclos
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN USUARIO
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.Property(u => u.NumeroDocumento).HasMaxLength(20).IsRequired();
                entity.Property(u => u.NombreCompleto).HasMaxLength(200).IsRequired();
                entity.HasIndex(u => u.NumeroDocumento);

                entity.HasOne(u => u.Rol)
                    .WithMany(r => r.Usuarios)
                    .HasForeignKey(u => u.RolID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN ROL
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<Rol>(entity =>
            {
                entity.Property(r => r.Descripcion).HasMaxLength(500);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN EXPEDIENTE
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<Expediente>(entity =>
            {
                entity.HasKey(e => e.ExpedienteID);

                // Propiedades básicas
                entity.Property(e => e.CodigoExpediente).HasMaxLength(20).IsRequired();
                entity.HasIndex(e => e.CodigoExpediente).IsUnique();

                entity.Property(e => e.TipoExpediente).HasMaxLength(20).IsRequired();
                entity.Property(e => e.HC).HasMaxLength(20).IsRequired();

                // Enum TipoDocumentoIdentidad → String
                entity.Property(e => e.TipoDocumento)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(e => e.NumeroDocumento).HasMaxLength(50).IsRequired();

                // Datos del paciente
                entity.Property(e => e.ApellidoPaterno).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ApellidoMaterno).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Nombres).HasMaxLength(100).IsRequired();
                entity.Property(e => e.NombreCompleto).HasMaxLength(300).IsRequired();

                entity.Property(e => e.Sexo).HasMaxLength(1).IsRequired();
                entity.Property(e => e.TipoSeguro).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ServicioFallecimiento).HasMaxLength(100).IsRequired();
                entity.Property(e => e.NumeroCama).HasMaxLength(20);

                // Datos médicos
                entity.Property(e => e.MedicoCertificaNombre).HasMaxLength(200).IsRequired();
                entity.Property(e => e.MedicoCMP).HasMaxLength(10).IsRequired();
                entity.Property(e => e.MedicoRNE).HasMaxLength(10);
                entity.Property(e => e.NumeroCertificadoSINADEF).HasMaxLength(50);
                entity.Property(e => e.DiagnosticoFinal).HasMaxLength(500);

                // Casos externos
                entity.Property(e => e.MedicoExternoNombre).HasMaxLength(200);
                entity.Property(e => e.MedicoExternoCMP).HasMaxLength(10);

                // Estado y QR
                entity.Property(e => e.EstadoActual)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.CodigoQR).HasMaxLength(50);

                // Auditoría
                entity.Property(e => e.MotivoEliminacion).HasMaxLength(500);

                // Índices
                entity.HasIndex(e => e.HC);
                entity.HasIndex(e => e.NumeroDocumento);
                entity.HasIndex(e => e.NumeroCertificadoSINADEF)
                    .IsUnique()
                    .HasFilter("[NumeroCertificadoSINADEF] IS NOT NULL");
                entity.HasIndex(e => e.CodigoQR)
                    .IsUnique()
                    .HasFilter("[CodigoQR] IS NOT NULL");

                // Relación con Usuario Creador
                entity.HasOne(e => e.UsuarioCreador)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioCreadorID)
                    .OnDelete(DeleteBehavior.Restrict);

                //  Bandeja Actual
                entity.HasOne(e => e.BandejaActual)
                    .WithMany()
                    .HasForeignKey(e => e.BandejaActualID)
                    .OnDelete(DeleteBehavior.Restrict);

                //  NUEVAS RELACIONES 1:1
                entity.HasOne(e => e.DeudaSangre)
                    .WithOne(d => d.Expediente)
                    .HasForeignKey<DeudaSangre>(d => d.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DeudaEconomica)
                    .WithOne(d => d.Expediente)
                    .HasForeignKey<DeudaEconomica>(d => d.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.VerificacionMortuorio)
                    .WithOne(v => v.Expediente)
                    .HasForeignKey<VerificacionMortuorio>(v => v.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ExpedienteLegal)
                    .WithOne(el => el.Expediente)
                    .HasForeignKey<ExpedienteLegal>(el => el.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.HistorialBandejas)
                    .WithOne(bh => bh.Expediente)
                    .HasForeignKey(bh => bh.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.DocumentosLegales)
                    .WithOne(dl => dl.Expediente)
                    .HasForeignKey(dl => dl.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.AutoridadesExternas)
                    .WithOne(ae => ae.Expediente)
                    .HasForeignKey(ae => ae.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN PERTENENCIA
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<Pertenencia>(entity =>
            {
                entity.HasKey(p => p.PertenenciaID);

                entity.Property(p => p.Descripcion).HasMaxLength(500).IsRequired();
                entity.Property(p => p.Estado).HasMaxLength(20).IsRequired();
                entity.Property(p => p.Observaciones).HasMaxLength(1000);

                entity.HasOne(p => p.Expediente)
                    .WithMany(e => e.Pertenencias)
                    .HasForeignKey(p => p.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN CUSTODIATRANSFERENCIA
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<CustodiaTransferencia>(entity =>
            {
                entity.HasKey(c => c.TransferenciaID);

                entity.Property(c => c.UbicacionOrigen).HasMaxLength(100).IsRequired();
                entity.Property(c => c.UbicacionDestino).HasMaxLength(100).IsRequired();
                entity.Property(c => c.Observaciones).HasMaxLength(1000);

                entity.HasOne(c => c.Expediente)
                    .WithMany(e => e.CustodiaTransferencias)
                    .HasForeignKey(c => c.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.UsuarioOrigen)
                    .WithMany()
                    .HasForeignKey(c => c.UsuarioOrigenID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.UsuarioDestino)
                    .WithMany()
                    .HasForeignKey(c => c.UsuarioDestinoID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN DEUDAECONOMICA
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<DeudaEconomica>(entity =>
            {
                entity.HasKey(d => d.DeudaEconomicaID);

                // Enum Estado → String
                entity.Property(d => d.Estado)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                // Enum TipoExoneracion → String
                entity.Property(d => d.TipoExoneracion)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                // Montos
                entity.Property(d => d.MontoDeuda).HasColumnType("decimal(10,2)");
                entity.Property(d => d.MontoExonerado).HasColumnType("decimal(10,2)");
                entity.Property(d => d.MontoPagado).HasColumnType("decimal(10,2)");

                // Strings
                entity.Property(d => d.NumeroBoleta).HasMaxLength(50);
                entity.Property(d => d.ObservacionesPago).HasMaxLength(500);
                entity.Property(d => d.ObservacionesExoneracion).HasMaxLength(1000);
                entity.Property(d => d.RutaPDFSustento).HasMaxLength(500);
                entity.Property(d => d.NombreArchivoSustento).HasMaxLength(255);

                // Relaciones
                entity.HasOne(d => d.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UsuarioActualizacion)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioActualizacionID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.AsistentaSocial)
                    .WithMany()
                    .HasForeignKey(d => d.AsistentaSocialID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            //  CONFIGURACIÓN DEUDASANGRE 
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<DeudaSangre>(entity =>
            {
                entity.HasKey(d => d.DeudaSangreID);

                // Enum Estado → String
                entity.Property(d => d.Estado)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                // Strings
                entity.Property(d => d.TipoSangre).HasMaxLength(10);
                entity.Property(d => d.JustificacionAnulacion).HasMaxLength(500);
                entity.Property(d => d.NombreFamiliarCompromiso).HasMaxLength(200);
                entity.Property(d => d.DNIFamiliarCompromiso).HasMaxLength(20);
                entity.Property(d => d.RutaPDFCompromiso).HasMaxLength(500);

                // Relaciones
                entity.HasOne(d => d.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UsuarioActualizacion)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioActualizacionID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.MedicoAnula)
                    .WithMany()
                    .HasForeignKey(d => d.MedicoAnulaID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN BANDEJA
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<Bandeja>(entity =>
            {
                entity.HasKey(b => b.BandejaID);

                entity.Property(b => b.Codigo).HasMaxLength(10).IsRequired();
                entity.HasIndex(b => b.Codigo).IsUnique();

                // Enum EstadoBandeja → String
                entity.Property(b => b.Estado)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(b => b.Observaciones).HasMaxLength(500);
                entity.Property(b => b.MotivoEliminacion).HasMaxLength(500);

                // Relación con Expediente (nullable)
                entity.HasOne(b => b.Expediente)
                    .WithMany()
                    .HasForeignKey(b => b.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con UsuarioAsigna (nullable)
                entity.HasOne(b => b.UsuarioAsigna)
                    .WithMany()
                    .HasForeignKey(b => b.UsuarioAsignaID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con UsuarioLibera (nullable)
                entity.HasOne(b => b.UsuarioLibera)
                    .WithMany()
                    .HasForeignKey(b => b.UsuarioLiberaID)
                    .OnDelete(DeleteBehavior.Restrict);

                //  NUEVA RELACIÓN: Historial de bandejas
                entity.HasMany(b => b.Historial)
                    .WithOne(bh => bh.Bandeja)
                    .HasForeignKey(bh => bh.BandejaID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            //  CONFIGURACIÓN BANDEJAHISTORIAL
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<BandejaHistorial>(entity =>
            {
                entity.HasKey(bh => bh.OcupacionID);

                // Enum AccionBandeja → String
                entity.Property(bh => bh.Accion)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(bh => bh.Observaciones).HasMaxLength(1000);

                // Índice compuesto
                entity.HasIndex(bh => new { bh.BandejaID, bh.FechaHoraSalida });

                // Relaciones
                entity.HasOne(bh => bh.UsuarioAsignador)
                    .WithMany()
                    .HasForeignKey(bh => bh.UsuarioAsignadorID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(bh => bh.UsuarioLibera)
                    .WithMany()
                    .HasForeignKey(bh => bh.UsuarioLiberaID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN VERIFICACIONMORTUORIO
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<VerificacionMortuorio>(entity =>
            {
                entity.HasKey(v => v.VerificacionID);

                entity.Property(v => v.HCBrazalete).HasMaxLength(20).IsRequired();
                entity.Property(v => v.TipoDocumentoBrazalete).HasMaxLength(20).IsRequired();
                entity.Property(v => v.NumeroDocumentoBrazalete).HasMaxLength(20).IsRequired();
                entity.Property(v => v.NombreCompletoBrazalete).HasMaxLength(300).IsRequired();
                entity.Property(v => v.ServicioBrazalete).HasMaxLength(100).IsRequired();
                entity.Property(v => v.CodigoExpedienteBrazalete).HasMaxLength(50).IsRequired();
                entity.Property(v => v.MotivoRechazo).HasMaxLength(500);
                entity.Property(v => v.Observaciones).HasMaxLength(1000);

                // Índices
                entity.HasIndex(v => v.ExpedienteID);
                entity.HasIndex(v => v.FechaHoraVerificacion);
                entity.HasIndex(v => new { v.ExpedienteID, v.Aprobada });

                // Relaciones
                entity.HasOne(v => v.Vigilante)
                    .WithMany()
                    .HasForeignKey(v => v.VigilanteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.TecnicoAmbulancia)
                    .WithMany()
                    .HasForeignKey(v => v.TecnicoAmbulanciaID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===================================================================
            // SALIDA MORTUORIO
            // ===================================================================

            modelBuilder.Entity<SalidaMortuorio>(entity =>
            {
                entity.ToTable("SalidasMortuorio");
                entity.HasKey(s => s.SalidaID);

                // Relación con Expediente
                entity.HasOne(s => s.Expediente)
                    .WithMany()
                    .HasForeignKey(s => s.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación con Vigilante
                entity.HasOne(s => s.Vigilante)
                    .WithMany()
                    .HasForeignKey(s => s.VigilanteID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con BandejaLiberada
                entity.HasOne(s => s.BandejaLiberada)
                    .WithMany()
                    .HasForeignKey(s => s.BandejaLiberadaID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación opcional con ActaRetiro (1:1)
                entity.HasOne(s => s.ActaRetiro)
                    .WithOne(a => a.SalidaMortuorio)
                    .HasForeignKey<SalidaMortuorio>(s => s.ActaRetiroID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación opcional con ExpedienteLegal (N:1)
                entity.HasOne(s => s.ExpedienteLegal)
                    .WithMany()
                    .HasForeignKey(s => s.ExpedienteLegalID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Conversión de Enum TipoSalida
                entity.Property(s => s.TipoSalida)
                    .HasConversion<int>()
                    .IsRequired();

                // Valores por defecto
                entity.Property(s => s.FechaHoraSalida)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(s => s.IncidenteRegistrado)
                    .HasDefaultValue(false);

                // Índices
                entity.HasIndex(s => s.ExpedienteID);
                entity.HasIndex(s => s.FechaHoraSalida);
                entity.HasIndex(s => s.TipoSalida);
                entity.HasIndex(s => s.ResponsableNumeroDocumento);
                entity.HasIndex(s => s.ActaRetiroID);
                entity.HasIndex(s => s.ExpedienteLegalID);
            });

            // ═══════════════════════════════════════════════════════════
            //  CONFIGURACIÓN DOCUMENTOLEGAL 
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<DocumentoLegal>(entity =>
            {
                entity.HasKey(d => d.DocumentoID);

                // Enum TipoDocumentoLegal → String
                entity.Property(d => d.TipoDocumento)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(d => d.RutaArchivo).HasMaxLength(500);
                entity.Property(d => d.NombreArchivo).HasMaxLength(255);
                entity.Property(d => d.Extension).HasMaxLength(10);
                entity.Property(d => d.Descripcion).HasMaxLength(500);
                entity.Property(d => d.ObservacionesValidacion).HasMaxLength(1000);

                // Relaciones
                entity.HasOne(d => d.UsuarioAdjunto)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioAdjuntoID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UsuarioValidador)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioValidadorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            //  CONFIGURACIÓN AUTORIDADEXTERNA 
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<AutoridadExterna>(entity =>
            {
                entity.HasKey(a => a.AutoridadID);

                // Enum TipoAutoridadExterna → String
                entity.Property(a => a.TipoAutoridad)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                // Enum TipoDocumentoIdentidad → String
                entity.Property(a => a.TipoDocumento)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(a => a.ApellidoPaterno).HasMaxLength(100).IsRequired();
                entity.Property(a => a.ApellidoMaterno).HasMaxLength(100).IsRequired();
                entity.Property(a => a.Nombres).HasMaxLength(100).IsRequired();
                entity.Property(a => a.NombreCompleto).HasMaxLength(300).IsRequired();
                entity.Property(a => a.NumeroDocumento).HasMaxLength(50).IsRequired();
                entity.Property(a => a.CodigoEspecial).HasMaxLength(50);
                entity.Property(a => a.Institucion).HasMaxLength(200).IsRequired();
                entity.Property(a => a.PlacaVehiculo).HasMaxLength(20);
                entity.Property(a => a.Telefono).HasMaxLength(20);
                entity.Property(a => a.Cargo).HasMaxLength(100);
                entity.Property(a => a.NumeroDocumentoOficial).HasMaxLength(100);
                entity.Property(a => a.Observaciones).HasMaxLength(1000);

                // Relaciones
                entity.HasOne(a => a.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            // EXPEDIENTE LEGAL
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<ExpedienteLegal>(entity =>
            {
                entity.ToTable("ExpedientesLegales");
                entity.HasKey(e => e.ExpedienteLegalID);

                // Relación 1:1 con Expediente
                entity.HasOne(e => e.Expediente)
                    .WithOne()
                    .HasForeignKey<ExpedienteLegal>(e => e.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Usuarios
                entity.HasOne(e => e.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UsuarioActualizacion)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioActualizacionID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UsuarioAdmision)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioAdmisionID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.JefeGuardia)
                    .WithMany()
                    .HasForeignKey(e => e.JefeGuardiaID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Colecciones
                entity.HasMany(e => e.Autoridades)
                    .WithOne(a => a.ExpedienteLegal)
                    .HasForeignKey(a => a.ExpedienteLegalID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Documentos)
                    .WithOne(d => d.ExpedienteLegal)
                    .HasForeignKey(d => d.ExpedienteLegalID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Conversión de Enum a Int
                entity.Property(e => e.Estado)
                    .HasConversion<int>()
                    .IsRequired()
                    .HasDefaultValue(EstadoExpedienteLegal.EnRegistro);

                // Valores por defecto
                entity.Property(e => e.ValidadoAdmision)
                    .HasDefaultValue(false);

                entity.Property(e => e.AutorizadoJefeGuardia)
                    .HasDefaultValue(false);

                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETDATE()");

                // Índices
                entity.HasIndex(e => e.ExpedienteID).IsUnique();
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.FechaCreacion);
            });

            // ===================================================================
            // ACTA DE RETIRO
            // ===================================================================
            modelBuilder.Entity<ActaRetiro>(entity =>
            {
                entity.ToTable("ActasRetiro");
                entity.HasKey(a => a.ActaRetiroID);

                // ═══════════════════════════════════════════════════════════
                // RELACIONES
                // ═══════════════════════════════════════════════════════════
                // Relación 1:1 con Expediente
                entity.HasOne(a => a.Expediente)
                    .WithOne()
                    .HasForeignKey<ActaRetiro>(a => a.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación 1:1 con SalidaMortuorio (inversa)
                entity.HasOne(a => a.SalidaMortuorio)
                    .WithOne(s => s.ActaRetiro)
                    .HasForeignKey<SalidaMortuorio>(s => s.ActaRetiroID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con UsuarioAdmision
                entity.HasOne(a => a.UsuarioAdmision)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioAdmisionID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con UsuarioSubidaPDF
                entity.HasOne(a => a.UsuarioSubidaPDF)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioSubidaPDFID)
                    .OnDelete(DeleteBehavior.Restrict);

                // ═══════════════════════════════════════════════════════════
                // CONVERSIÓN DE ENUMS
                // ═══════════════════════════════════════════════════════════
                // Tipo de salida (siempre requerido)
                entity.Property(a => a.TipoSalida)
                    .HasConversion<int>()
                    .IsRequired()
                    .HasDefaultValue(TipoSalida.Familiar);

                // Tipo de documento del fallecido (siempre requerido)
                entity.Property(a => a.TipoDocumentoFallecido)
                    .HasConversion<int>()
                    .IsRequired();

                // Tipo de documento del familiar (NULLABLE - solo si Familiar)
                entity.Property(a => a.FamiliarTipoDocumento)
                    .HasConversion<int?>()
                    .IsRequired(false);

                // Tipo de autoridad (NULLABLE - solo si AutoridadLegal)
                entity.Property(a => a.TipoAutoridad)
                    .HasConversion<int?>()
                    .IsRequired(false);

                // Tipo de documento de la autoridad (NULLABLE - solo si AutoridadLegal)
                entity.Property(a => a.AutoridadTipoDocumento)
                    .HasConversion<int?>()
                    .IsRequired(false);

                // ═══════════════════════════════════════════════════════════
                // VALORES POR DEFECTO
                // ═══════════════════════════════════════════════════════════
                entity.Property(a => a.FirmadoResponsable)
                    .HasDefaultValue(false);

                entity.Property(a => a.FirmadoAdmisionista)
                    .HasDefaultValue(false);

                entity.Property(a => a.FirmadoSupervisorVigilancia)
                    .HasDefaultValue(false);

                entity.Property(a => a.FechaRegistro)
                    .HasDefaultValueSql("GETDATE()");

                // ═══════════════════════════════════════════════════════════
                // ÍNDICES PARA BÚSQUEDAS OPTIMIZADAS
                // ═══════════════════════════════════════════════════════════
                // Expediente (único - relación 1:1)
                entity.HasIndex(a => a.ExpedienteID)
                    .IsUnique();

                // Documentos legales
                entity.HasIndex(a => a.NumeroCertificadoDefuncion);
                entity.HasIndex(a => a.NumeroOficioLegal);

                // Documentos de responsables
                entity.HasIndex(a => a.FamiliarNumeroDocumento);
                entity.HasIndex(a => a.AutoridadNumeroDocumento);

                // Filtros por tipo y fecha
                entity.HasIndex(a => a.TipoSalida);
                entity.HasIndex(a => a.TipoAutoridad);
                entity.HasIndex(a => a.FechaRegistro);

                // Índice compuesto para búsquedas frecuentes
                entity.HasIndex(a => new { a.TipoSalida, a.FechaRegistro });
            });

            // ═══════════════════════════════════════════════════════════
            // AUTORIDAD EXTERNA
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<AutoridadExterna>(entity =>
            {
                entity.ToTable("AutoridadesExternas");
                entity.HasKey(a => a.AutoridadID);

                // Relación con Expediente
                entity.HasOne(a => a.Expediente)
                    .WithMany()
                    .HasForeignKey(a => a.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación con ExpedienteLegal (opcional)
                entity.HasOne(a => a.ExpedienteLegal)
                    .WithMany(el => el.Autoridades)
                    .HasForeignKey(a => a.ExpedienteLegalID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Usuario
                entity.HasOne(a => a.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Conversión de Enums
                entity.Property(a => a.TipoAutoridad)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(a => a.TipoDocumento)
                    .HasConversion<int>()
                    .IsRequired();

                // Valores por defecto
                entity.Property(a => a.FechaHoraLlegada)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(a => a.FechaRegistro)
                    .HasDefaultValueSql("GETDATE()");

                // Índices
                entity.HasIndex(a => a.ExpedienteID);
                entity.HasIndex(a => a.ExpedienteLegalID);
                entity.HasIndex(a => a.TipoAutoridad);
                entity.HasIndex(a => a.NumeroDocumento);
                entity.HasIndex(a => a.FechaHoraLlegada);
            });

            // ═══════════════════════════════════════════════════════════
            // DOCUMENTO LEGAL
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<DocumentoLegal>(entity =>
            {
                entity.ToTable("DocumentosLegales");
                entity.HasKey(d => d.DocumentoID);

                // Relación con Expediente
                entity.HasOne(d => d.Expediente)
                    .WithMany()
                    .HasForeignKey(d => d.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación con ExpedienteLegal (opcional)
                entity.HasOne(d => d.ExpedienteLegal)
                    .WithMany(el => el.Documentos)
                    .HasForeignKey(d => d.ExpedienteLegalID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Usuarios
                entity.HasOne(d => d.UsuarioAdjunto)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioAdjuntoID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UsuarioValidador)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioValidadorID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Conversión de Enum
                entity.Property(d => d.TipoDocumento)
                    .HasConversion<int>()
                    .IsRequired();

                // Valores por defecto
                entity.Property(d => d.Adjuntado)
                    .HasDefaultValue(false);

                entity.Property(d => d.Validado)
                    .HasDefaultValue(false);

                // Índices
                entity.HasIndex(d => d.ExpedienteID);
                entity.HasIndex(d => d.ExpedienteLegalID);
                entity.HasIndex(d => d.TipoDocumento);
                entity.HasIndex(d => d.FechaAdjunto);
            });
            

        // ═══════════════════════════════════════════════════════════
        // CONFIGURACIÓN AUDITLOG
        // ═══════════════════════════════════════════════════════════

        modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.LogID);

                entity.Property(a => a.Modulo).HasMaxLength(50).IsRequired();
                entity.Property(a => a.Accion).HasMaxLength(100).IsRequired();
                entity.Property(a => a.IPOrigen).HasMaxLength(50);
                entity.Property(a => a.Observaciones).HasMaxLength(1000);

                entity.HasIndex(a => a.FechaHora);
                entity.HasIndex(a => new { a.Modulo, a.Accion });

                entity.HasOne(a => a.Expediente)
                    .WithMany()
                    .HasForeignKey(a => a.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Usuario)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ═══════════════════════════════════════════════════════════
            // CONFIGURACIÓN SOLICITUDCORRECCIONEXPEDIENTE
            // ═══════════════════════════════════════════════════════════

            modelBuilder.Entity<SolicitudCorreccionExpediente>(entity =>
            {
                entity.HasKey(sc => sc.SolicitudID);

                entity.Property(sc => sc.DatosIncorrectos).HasMaxLength(2000).IsRequired();
                entity.Property(sc => sc.DescripcionProblema).HasMaxLength(1000).IsRequired();
                entity.Property(sc => sc.ObservacionesSolicitud).HasMaxLength(1000);
                entity.Property(sc => sc.DescripcionResolucion).HasMaxLength(1000);
                entity.Property(sc => sc.ObservacionesResolucion).HasMaxLength(1000);

                // Índices
                entity.HasIndex(sc => sc.ExpedienteID);
                entity.HasIndex(sc => sc.Resuelta);
                entity.HasIndex(sc => sc.FechaHoraSolicitud);
                entity.HasIndex(sc => new { sc.Resuelta, sc.FechaHoraSolicitud });

                // Relaciones
                entity.HasOne(sc => sc.Expediente)
                    .WithMany()
                    .HasForeignKey(sc => sc.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sc => sc.UsuarioSolicita)
                    .WithMany()
                    .HasForeignKey(sc => sc.UsuarioSolicitaID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sc => sc.UsuarioResponsable)
                    .WithMany()
                    .HasForeignKey(sc => sc.UsuarioResponsableID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}