using HospitadentApi.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    return new ClinicRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});
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
    return new ClinicRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});
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
    return new UserWorkingScheduleRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    return new AppointmentRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});

builder.Services.AddScoped<IRepository<HospitadentApi.Entity.DoctorBranchCode>>(sp => sp.GetRequiredService<DoctorBranchCodeRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Clinic>>(sp => sp.GetRequiredService<ClinicRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.User>>(sp => sp.GetRequiredService<UserRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.UserWorkingSchedule>>(sp => sp.GetRequiredService<UserWorkingScheduleRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Appointment>>(sp => sp.GetRequiredService<AppointmentRepository>());

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