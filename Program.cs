using Supabase;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

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

builder.Services.AddControllers();

builder.Services.AddScoped(typeof(ISupabaseService<>), typeof(SupabaseService<>));
builder.Services.AddScoped<IAuthAppService, AuthAppService>();
builder.Services.AddScoped<IProfilesAppService, ProfilesAppService>();
builder.Services.AddScoped<IGroupsAppService, GroupsAppService>();
builder.Services.AddScoped<IRequestsAppService, RequestsAppService>();
builder.Services.AddScoped<ISubjectsAppService, SubjectsAppService>();
builder.Services.AddScoped<ILocationsAppService, LocationsAppService>();
builder.Services.AddScoped<ISchedulesAppService, SchedulesAppService>();
builder.Services.AddScoped<IAlgorithmsAppService, AlgorithmsAppService>();

// ✅ CORS CORRECTO PARA COOKIES
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173") // tu frontend real
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("Frontend"); // 👈 antes de MapControllers

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();