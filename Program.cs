using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SisMortuorio.Business.Hubs;
using SisMortuorio.Business.Services;
using SisMortuorio.Business.Workers;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.ExternalSystems;
using SisMortuorio.Data.Repositories;
using SisMortuorio.Data.Seeders;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// 1. CONFIGURAR DbContext
// ===================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ===================================================================
// 2. CONFIGURAR Identity
// ===================================================================
builder.Services.AddIdentity<Usuario, Rol>(options =>
{
    // Configuración de contraseñas (relajada para desarrollo)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;

    // Configuración de usuarios
    options.User.RequireUniqueEmail = false;

    // Deshabilitar confirmación de email
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ===================================================================
// 3. CONFIGURAR JWT Authentication
// ===================================================================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey no configurada en User Secrets");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // En producción cambiar a true
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Configurar autenticación JWT para SignalR
    // SignalR puede enviar el token en query string (?access_token=...)
    // o en el header Authorization estándar
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Leer el token desde el query string si existe
            var accessToken = context.Request.Query["access_token"];

            // Si el request es para el Hub de SignalR
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/sgmhub"))
            {
                // Asignar el token al contexto para que JWT lo valide
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// ===================================================================
// 4. REGISTRAR SERVICIOS DE NEGOCIO
// ===================================================================

// Servicios principales
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExpedienteService, ExpedienteService>();
builder.Services.AddScoped<IQRService, QRService>();
builder.Services.AddScoped<IBrazaleteService, BrazaleteService>();
builder.Services.AddScoped<ICustodiaService, CustodiaService>();

// Repositorios
builder.Services.AddScoped<IExpedienteRepository, ExpedienteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ICustodiaRepository, CustodiaRepository>();

// Repositorios Entrada-Salida Mortuorio-SolicitudDeCorrecion
builder.Services.AddScoped<IBandejaRepository, BandejaRepository>();
builder.Services.AddScoped<IBandejaHistorialRepository, BandejaHistorialRepository>();
builder.Services.AddScoped<IVerificacionMortuorioRepository, VerificacionMortuorioRepository>();
builder.Services.AddScoped<ISalidaMortuorioRepository, SalidaMortuorioRepository>();
builder.Services.AddScoped<ISolicitudCorreccionRepository, SolicitudCorreccionRepository>();
builder.Services.AddScoped<IDocumentoLegalRepository, DocumentoLegalRepository>();
builder.Services.AddScoped<IExpedienteLegalRepository, ExpedienteLegalRepository>();
builder.Services.AddScoped<IAutoridadExternaRepository, AutoridadExternaRepository>();
builder.Services.AddScoped<IActaRetiroRepository, ActaRetiroRepository>();

// Repositories - Deudas
builder.Services.AddScoped<IDeudaSangreRepository, DeudaSangreRepository>();
builder.Services.AddScoped<IDeudaEconomicaRepository, DeudaEconomicaRepository>();

// Servicios FASE 4.5
builder.Services.AddScoped<IVerificacionService, VerificacionService>();
builder.Services.AddScoped<IBandejaService, BandejaService>();
builder.Services.AddScoped<ISalidaMortuorioService, SalidaMortuorioService>();
builder.Services.AddScoped<ISolicitudCorreccionService, SolicitudCorreccionService>();

// Servicios de sistemas externos
builder.Services.AddScoped<IGalenhosService, GalenhosService>();
builder.Services.AddScoped<ISigemService, SigemService>();
builder.Services.AddScoped<IIntegracionService, IntegracionService>();

// Servicio de máquina de estados
builder.Services.AddScoped<IStateMachineService, StateMachineService>();

// Servicio de Mapeo
builder.Services.AddScoped<IExpedienteMapperService, ExpedienteMapperService>();

// Servicio de tracking de conexiones SignalR (Singleton)
// Singleton = una sola instancia compartida en toda la aplicación
builder.Services.AddSingleton<IConnectionTrackerService, ConnectionTrackerService>();

// Servicios de negocio - Deudas
builder.Services.AddScoped<IDeudaSangreService, DeudaSangreService>();
builder.Services.AddScoped<IDeudaEconomicaService, DeudaEconomicaService>();

// Servicios de negocio - Expedientes Legales
builder.Services.AddScoped<IExpedienteLegalService, ExpedienteLegalService>();
// Servicio de negocio - Acta de Retiro
builder.Services.AddScoped<IActaRetiroService, ActaRetiroService>();
// Servicio de almacenamiento de archivos
builder.Services.AddScoped<ILocalFileStorageService, LocalFileStorageService>();

// Servicio centralizado de notificaciones de deudas
builder.Services.AddScoped<INotificacionDeudaService, NotificacionDeudaService>();

// Servicio centralizado de notificaciones de bandejas
builder.Services.AddScoped<INotificacionBandejaService, NotificacionBandejaService>();

// Servicio Para Generar PDF
builder.Services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
// ===================================================================
// 5. CONFIGURAR Controllers
// ===================================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Esto permite convertir strings ("Familiar") a Enums automáticamente
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ===================================================================
// 6. CONFIGURAR SignalR (FASE 2 )
// ===================================================================
builder.Services.AddSignalR(options =>
{
    // Keepalive Interval: Cada cuánto el servidor envía un "ping" al cliente
    // Default: 15 segundos. Reducido a 10 para detectar desconexiones más rápido.
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);

    // Client Timeout: Tiempo máximo sin respuesta del cliente antes de considerarlo desconectado
    // Default: 30 segundos. Aumentado a 60 para conexiones lentas (3G/4G).
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);

    // Handshake Timeout: Tiempo máximo para completar el handshake inicial
    // Default: 15 segundos. Suficiente para conexiones lentas.
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);

    // Max Message Size: Tamaño máximo de mensaje en bytes
    // Default: 32KB. Aumentado a 128KB para soportar notificaciones con datos grandes.
    options.MaximumReceiveMessageSize = 128 * 1024; // 128KB

    // Enable Detailed Errors: Mostrar errores detallados en desarrollo
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ===================================================================
// 7. REGISTRAR WORKERS (Background Services)
// ===================================================================

// Worker: Alerta de permanencia (cada 60 min)
builder.Services.AddHostedService<PermanenciaAlertWorker>();

// Worker: Alerta de solicitudes vencidas (cada 15 min)
builder.Services.AddHostedService<SolicitudAlertWorker>();

// Worker para limpiar conexiones zombies (cada 30 min)
// Este worker llama a ConnectionTracker.CleanupStaleConnectionsAsync()
// para remover conexiones que llevan >60 min sin actividad
builder.Services.AddHostedService<ConnectionCleanupWorker>();

// ===================================================================
// 8. CONFIGURAR Swagger con JWT
// ===================================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SGM API - Sistema de Gestión de Mortuorio",
        Version = "v1",
        Description = "API para gestión de expedientes de fallecidos con soporte SignalR"
    });

    // Configurar autenticación JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el token JWT en el formato: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===================================================================
// 9. CONFIGURAR CORS
// ===================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        policy =>
        {
            // ⭐ IMPORTANTE: SignalR requiere AllowCredentials para WebSockets
            policy.WithOrigins(
                    "http://localhost:4200",  // Angular dev server
                    "https://localhost:4200"  // Angular dev server HTTPS
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); //CRÍTICO para SignalR WebSockets
        });

    // Política permisiva solo para desarrollo
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// ===================================================================
// 10. BUILD APP
// ===================================================================
var app = builder.Build();

// ===================================================================
// 11. EJECUTAR SEEDER EN DESARROLLO
// ===================================================================
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var roleManager = services.GetRequiredService<RoleManager<Rol>>();
        var environment = services.GetRequiredService<IWebHostEnvironment>();

        await DataSeeder.SeedAsync(context, userManager, roleManager, environment);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al ejecutar seeding de datos");
    }
}

// ===================================================================
// 12. CONFIGURAR HTTP PIPELINE
// ===================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SGM API v1");
    });
}

app.UseStaticFiles();
app.UseHttpsRedirection();

// Usar política CORS específica para Angular en producción
// En desarrollo usa "AllowAll" por simplicidad
app.UseCors(app.Environment.IsDevelopment() ? "AllowAll" : "AllowAngular");

// El orden importa (Authentication antes de Authorization)
app.UseAuthentication();  // ← Primero autenticación (valida JWT)
app.UseAuthorization();   // ← Luego autorización (valida permisos)

app.MapControllers();

// Mapear el Hub de SignalR en la ruta /sgmhub
// El cliente Angular se conectará a: https://localhost:7153/sgmhub
app.MapHub<SgmHub>("/sgmhub");

app.Run();  // ← ESTO SIEMPRE AL FINAL