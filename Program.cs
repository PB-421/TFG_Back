using Supabase;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Services.AddSingleton(provider =>
{
    var config = builder.Configuration.GetSection("Supabase");
    return new Client(
        Environment.GetEnvironmentVariable("API_URL")!,
        Environment.GetEnvironmentVariable("API_SUDOKEY")!,
        new SupabaseOptions
        {
            AutoConnectRealtime = false
        }
    );
});

builder.Services.AddControllers();
//Cuando se llama a la interfaz, sabe a que llamar
builder.Services.AddScoped(typeof(ISupabaseService<>), typeof(SupabaseService<>));
builder.Services.AddScoped<IGroupsAppService, GroupsAppService>();
builder.Services.AddScoped<IRequestsAppService, RequestsAppService>();
builder.Services.AddScoped<ISubjectsAppService, SubjectsAppService>();
builder.Services.AddScoped<ILocationsAppService, LocationsAppService>();
builder.Services.AddScoped<ISchedulesAppService, SchedulesAppService>();

// CONFIGURAR CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");
app.MapControllers();
app.Run();