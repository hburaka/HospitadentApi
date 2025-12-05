using HospitadentApi.Repository;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// configure logging (console + default settings)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    return new ClinicRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});
//builder.Services.AddScoped(sp =>
//{
//    var cfg = sp.GetRequiredService<IConfiguration>();
//    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
//    return new DoctorBranchCodeRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
//});
//builder.Services.AddScoped(sp =>
//{
//    var cfg = sp.GetRequiredService<IConfiguration>();
//    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
//    return new ClinicRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
//});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    return new DoctorBranchCodeRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    var clinicRepo = sp.GetRequiredService<ClinicRepository>();
    return new UserRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."), clinicRepo);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<UserWorkingScheduleRepository>>();
    return new UserWorkingScheduleRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."), logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<AppointmentRepository>>();
    return new AppointmentRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."), logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<PatientRepository>>();
    return new PatientRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."), logger);
});

builder.Services.AddScoped<IRepository<HospitadentApi.Entity.DoctorBranchCode>>(sp => sp.GetRequiredService<DoctorBranchCodeRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Clinic>>(sp => sp.GetRequiredService<ClinicRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.User>>(sp => sp.GetRequiredService<UserRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.UserWorkingSchedule>>(sp => sp.GetRequiredService<UserWorkingScheduleRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Appointment>>(sp => sp.GetRequiredService<AppointmentRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Patient>>(sp => sp.GetRequiredService<PatientRepository>());

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();