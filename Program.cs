using Supabase;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

// 2. Configurar el puerto dinámico para Render/Docker
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 3. Configurar Cliente Supabase
builder.Services.AddScoped<Client>(provider =>
{
    return new Client(
        Environment.GetEnvironmentVariable("DB_URL")!,
        Environment.GetEnvironmentVariable("DB_SUDOKEY")!,
        new SupabaseOptions
        {
            AutoConnectRealtime = false,
            AutoRefreshToken = false
        }
    );
});

// 4. Configuración de Autenticación (Para que IsAuthenticated funcione)
// Supabase usa JWT. Validamos el token que viene del frontend.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("DB_URL") + "/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")!)),
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudience = "authenticated"
        };
    });

// 5. Inyección de Dependencias
builder.Services.AddControllers();
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddScoped<IProfilesAppService, ProfilesAppService>();
builder.Services.AddScoped<IGroupsAppService, GroupsAppService>();
builder.Services.AddScoped<IRequestsAppService, RequestsAppService>();
builder.Services.AddScoped<ISubjectsAppService, SubjectsAppService>();
builder.Services.AddScoped<ILocationsAppService, LocationsAppService>();
builder.Services.AddScoped<ISchedulesAppService, SchedulesAppService>();
builder.Services.AddScoped<IAlgorithmsAppService, AlgorithmsAppService>();
builder.Services.AddScoped<IControlAppService, ControlAppService>();

// 6. CORS (Configurar para desarrollo y producción)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "https://tfg-front-rho.vercel.app" 
            ) 
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 7. Pipeline de Middleware (EL ORDEN IMPORTA)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();