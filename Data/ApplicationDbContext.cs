using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SisMortuorio.Data.Entities;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // CRÍTICO: Deshabilitar cascadas en Identity para evitar ciclos
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
                entity.Property(e => e.CausaMuerte).HasMaxLength(500);

                entity.Property(e => e.EstadoActual).HasMaxLength(50).IsRequired();
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

                entity.Property(o => o.BandejaID).HasMaxLength(10).IsRequired();
                entity.HasIndex(o => new { o.BandejaID, o.FechaHoraSalida });

                entity.HasOne(o => o.Expediente)
                    .WithMany()
                    .HasForeignKey(o => o.ExpedienteID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.UsuarioAsignador)
                    .WithMany()
                    .HasForeignKey(o => o.UsuarioAsignadorID)
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
        }
    }
}