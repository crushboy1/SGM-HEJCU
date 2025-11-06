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

        // DbSets existentes
        public DbSet<Expediente> Expedientes { get; set; }
        public DbSet<Pertenencia> Pertenencias { get; set; }
        public DbSet<CustodiaTransferencia> CustodiaTransferencias { get; set; }

        public DbSet<DeudaEconomica> DeudasEconomicas { get; set; }
        public DbSet<DeudaSangre> DeudasSangre { get; set; }
        public DbSet<OcupacionBandeja> OcupacionesBandejas { get; set; }
        public DbSet<DocumentoLegal> DocumentosLegales { get; set; }
        public DbSet<AutoridadExterna> AutoridadesExternas { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<Bandeja> Bandejas { get; set; }
        public DbSet<VerificacionMortuorio> VerificacionesMortuorio { get; set; }
        public DbSet<SalidaMortuorio> SalidasMortuorio { get; set; }
        public DbSet<SolicitudCorreccionExpediente> SolicitudesCorreccion { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  Deshabilitar cascadas en Identity para evitar ciclos
            foreach (var relationship in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Configuración Usuario
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

            // Configuración Rol
            modelBuilder.Entity<Rol>(entity =>
            {
                entity.Property(r => r.Descripcion).HasMaxLength(500);
            });

            // Configuración Expediente
            modelBuilder.Entity<Expediente>(entity =>
            {
                entity.HasKey(e => e.ExpedienteID);

                entity.Property(e => e.CodigoExpediente).HasMaxLength(50).IsRequired();
                entity.HasIndex(e => e.CodigoExpediente).IsUnique();

                entity.Property(e => e.TipoExpediente).HasMaxLength(20).IsRequired();
                entity.Property(e => e.HC).HasMaxLength(20).IsRequired();
                entity.Property(e => e.NumeroDocumento).HasMaxLength(20).IsRequired();

                entity.Property(e => e.ApellidoPaterno).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ApellidoMaterno).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Nombres).HasMaxLength(100).IsRequired();
                entity.Property(e => e.NombreCompleto).HasMaxLength(300).IsRequired();

                entity.Property(e => e.Sexo).HasMaxLength(1).IsRequired();
                entity.Property(e => e.TipoSeguro).HasMaxLength(20).IsRequired();
                entity.Property(e => e.ServicioFallecimiento).HasMaxLength(100).IsRequired();
                entity.Property(e => e.NumeroCama).HasMaxLength(20);

                entity.Property(e => e.MedicoCertificaNombre).HasMaxLength(200).IsRequired();
                entity.Property(e => e.MedicoCMP).HasMaxLength(10).IsRequired();
                entity.Property(e => e.MedicoRNE).HasMaxLength(20);
                entity.Property(e => e.NumeroCertificadoSINADEF).HasMaxLength(50);
                entity.Property(e => e.DiagnosticoFinal).HasMaxLength(500);

                entity.Property(e => e.EstadoActual).HasConversion<string>().HasMaxLength(50).IsRequired();
                entity.Property(e => e.CodigoQR).HasMaxLength(100);

                entity.Property(e => e.MotivoEliminacion).HasMaxLength(500);

                entity.HasIndex(e => e.HC);
                entity.HasIndex(e => e.NumeroDocumento);
                entity.HasIndex(e => e.NumeroCertificadoSINADEF).IsUnique();
                entity.HasIndex(e => e.CodigoQR).IsUnique();

                entity.HasOne(e => e.UsuarioCreador)
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioCreadorID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración Pertenencia
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

            // Configuración CustodiaTransferencia
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

            // Configuración DeudaEconomica
            modelBuilder.Entity<DeudaEconomica>(entity =>
            {
                entity.HasKey(d => d.DeudaEconomicaID);

                entity.Property(d => d.MontoDeuda).HasColumnType("decimal(18,2)");
                entity.Property(d => d.NumeroBoleta).HasMaxLength(50);
                entity.Property(d => d.PorcentajeExoneracion).HasColumnType("decimal(5,2)");
                entity.Property(d => d.ObservacionesExoneracion).HasMaxLength(1000);

                entity.HasOne(d => d.Expediente)
                    .WithMany()
                    .HasForeignKey(d => d.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UsuarioActualizacion)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioActualizacionID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración DeudaSangre
            modelBuilder.Entity<DeudaSangre>(entity =>
            {
                entity.HasKey(d => d.DeudaSangreID);

                entity.Property(d => d.Detalle).HasMaxLength(500);

                entity.HasOne(d => d.Expediente)
                    .WithMany()
                    .HasForeignKey(d => d.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UsuarioActualizacion)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioActualizacionID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración OcupacionBandeja
            modelBuilder.Entity<OcupacionBandeja>(entity =>
            {
                entity.HasKey(o => o.OcupacionID);

                // BandejaID ahora es int (FK a Bandeja)
                entity.Property(o => o.BandejaID).IsRequired();

                // Conversión del enum AccionBandeja a string
                entity.Property(o => o.Accion)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(o => o.Observaciones).HasMaxLength(1000);

                // Índice compuesto
                entity.HasIndex(o => new { o.BandejaID, o.FechaHoraSalida });

                //  Relación con Bandeja
                entity.HasOne(o => o.Bandeja)
                    .WithMany()
                    .HasForeignKey(o => o.BandejaID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Expediente)
                    .WithMany()
                    .HasForeignKey(o => o.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.UsuarioAsignador)
                    .WithMany()
                    .HasForeignKey(o => o.UsuarioAsignadorID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con UsuarioLibera
                entity.HasOne(o => o.UsuarioLibera)
                    .WithMany()
                    .HasForeignKey(o => o.UsuarioLiberaID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración DocumentoLegal
            modelBuilder.Entity<DocumentoLegal>(entity =>
            {
                entity.HasKey(d => d.DocumentoID);

                entity.Property(d => d.RutaArchivo).HasMaxLength(500);

                entity.HasOne(d => d.Expediente)
                    .WithMany()
                    .HasForeignKey(d => d.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.UsuarioAdjunto)
                    .WithMany()
                    .HasForeignKey(d => d.UsuarioAdjuntoID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración AutoridadExterna
            modelBuilder.Entity<AutoridadExterna>(entity =>
            {
                entity.HasKey(a => a.AutoridadID);

                entity.Property(a => a.Nombre).HasMaxLength(200).IsRequired();
                entity.Property(a => a.NumeroDocumento).HasMaxLength(20).IsRequired();
                entity.Property(a => a.CodigoEspecial).HasMaxLength(50);
                entity.Property(a => a.Institucion).HasMaxLength(200).IsRequired();
                entity.Property(a => a.PlacaVehiculo).HasMaxLength(20);

                entity.HasOne(a => a.Expediente)
                    .WithMany()
                    .HasForeignKey(a => a.ExpedienteID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.UsuarioRegistro)
                    .WithMany()
                    .HasForeignKey(a => a.UsuarioRegistroID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.LogID);

                entity.Property(a => a.Modulo).HasMaxLength(100).IsRequired();
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
            // Configuración Bandeja
            modelBuilder.Entity<Bandeja>(entity =>
            {
                entity.HasKey(b => b.BandejaID);

                entity.Property(b => b.Codigo).HasMaxLength(10).IsRequired();
                entity.HasIndex(b => b.Codigo).IsUnique();

                // Conversión del enum EstadoBandeja a string
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
            });

            // Configuración VerificacionMortuorio
            modelBuilder.Entity<VerificacionMortuorio>(entity =>
            {
                entity.HasKey(v => v.VerificacionID);

                entity.Property(v => v.HCBrazalete).HasMaxLength(20).IsRequired();
                entity.Property(v => v.DNIBrazalete).HasMaxLength(20).IsRequired();
                entity.Property(v => v.NombreCompletoBrazalete).HasMaxLength(300).IsRequired();
                entity.Property(v => v.ServicioBrazalete).HasMaxLength(100).IsRequired();
                entity.Property(v => v.CodigoExpedienteBrazalete).HasMaxLength(50).IsRequired();
                entity.Property(v => v.MotivoRechazo).HasMaxLength(500);
                entity.Property(v => v.Observaciones).HasMaxLength(1000);

                // Índices para búsquedas
                entity.HasIndex(v => v.ExpedienteID);
                entity.HasIndex(v => v.FechaHoraVerificacion);
                entity.HasIndex(v => new { v.ExpedienteID, v.Aprobada });

                // Relaciones
                entity.HasOne(v => v.Expediente)
                    .WithMany()
                    .HasForeignKey(v => v.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.Vigilante)
                    .WithMany()
                    .HasForeignKey(v => v.VigilanteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(v => v.TecnicoAmbulancia)
                    .WithMany()
                    .HasForeignKey(v => v.TecnicoAmbulanciaID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración SalidaMortuorio
            modelBuilder.Entity<SalidaMortuorio>(entity =>
            {
                entity.HasKey(s => s.SalidaID);

                // Conversión del enum TipoSalida a string
                entity.Property(s => s.TipoSalida)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(s => s.ResponsableNombre).HasMaxLength(200).IsRequired();
                entity.Property(s => s.ResponsableTipoDocumento).HasMaxLength(20).IsRequired();
                entity.Property(s => s.ResponsableNumeroDocumento).HasMaxLength(20).IsRequired();
                entity.Property(s => s.ResponsableParentesco).HasMaxLength(50);
                entity.Property(s => s.ResponsableTelefono).HasMaxLength(20);
                entity.Property(s => s.NumeroAutorizacion).HasMaxLength(100);
                entity.Property(s => s.EntidadAutorizante).HasMaxLength(200);
                entity.Property(s => s.NumeroRecibo).HasMaxLength(50);
                entity.Property(s => s.NombreFuneraria).HasMaxLength(200);
                entity.Property(s => s.ConductorFuneraria).HasMaxLength(200);
                entity.Property(s => s.DNIConductor).HasMaxLength(20);
                entity.Property(s => s.PlacaVehiculo).HasMaxLength(20);
                entity.Property(s => s.Destino).HasMaxLength(200);
                entity.Property(s => s.Observaciones).HasMaxLength(1000);
                entity.Property(s => s.DetalleIncidente).HasMaxLength(1000);

                // Índices
                entity.HasIndex(s => s.ExpedienteID).IsUnique(); // Un expediente solo puede tener una salida
                entity.HasIndex(s => s.FechaHoraSalida);
                entity.HasIndex(s => s.TipoSalida);
                entity.HasIndex(s => s.ResponsableNumeroDocumento);

                // Relaciones
                entity.HasOne(s => s.Expediente)
                    .WithMany()
                    .HasForeignKey(s => s.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Vigilante)
                    .WithMany()
                    .HasForeignKey(s => s.VigilanteID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración SolicitudCorreccionExpediente
            modelBuilder.Entity<SolicitudCorreccionExpediente>(entity =>
            {
                entity.HasKey(sc => sc.SolicitudID);

                entity.Property(sc => sc.DatosIncorrectos).HasMaxLength(2000).IsRequired();
                entity.Property(sc => sc.DescripcionProblema).HasMaxLength(1000).IsRequired();
                entity.Property(sc => sc.ObservacionesSolicitud).HasMaxLength(1000);
                entity.Property(sc => sc.DescripcionResolucion).HasMaxLength(1000);
                entity.Property(sc => sc.ObservacionesResolucion).HasMaxLength(1000);

                // Índices para reportes y búsquedas
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