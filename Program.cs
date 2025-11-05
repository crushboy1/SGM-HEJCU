using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SisMortuorio.Business.Services;
using SisMortuorio.Data;
using SisMortuorio.Data.Entities;
using SisMortuorio.Data.ExternalSystems;
using SisMortuorio.Data.Repositories;
using SisMortuorio.Data.Seeders;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurar Identity
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

// 3. Configurar JWT Authentication
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
});

// 4. Registrar servicios de negocio
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IExpedienteService, ExpedienteService>();
builder.Services.AddScoped<IQRService, QRService>();
builder.Services.AddScoped<IBrazaleteService, BrazaleteService>();
builder.Services.AddScoped<ICustodiaService, CustodiaService>();
// Repositorios
builder.Services.AddScoped<IExpedienteRepository, ExpedienteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ICustodiaRepository, CustodiaRepository>();
// Servicios de sistemas externos
builder.Services.AddScoped<IGalenhosService, GalenhosService>();
builder.Services.AddScoped<ISigemService, SigemService>();
builder.Services.AddScoped<IIntegracionService, IntegracionService>();
// Servicio de máquina de estados
builder.Services.AddScoped<IStateMachineService, StateMachineService>();
// Se registra el nuevo servicio de Mapeo
builder.Services.AddScoped<IExpedienteMapperService, ExpedienteMapperService>();
// 5. Configurar Controllers
builder.Services.AddControllers();

// 6. Configurar Swagger con JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SGM API - Sistema de Gestión de Mortuorio",
        Version = "v1",
        Description = "API para gestión de expedientes de fallecidos"
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

// 7. CORS (para futuro frontend)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// 8. Ejecutar Seeder en desarrollo (ANTES de configurar pipeline)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
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
}

// 9. Configure the HTTP request pipeline
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
app.UseCors("AllowAll");

// IMPORTANTE: El orden importa
app.UseAuthentication();  // ← Primero autenticación
app.UseAuthorization();   // ← Luego autorización

app.MapControllers();

app.Run();  // ← ESTO SIEMPRE AL FINAL