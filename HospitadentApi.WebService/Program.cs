using HospitadentApi.Repository;

var builder = WebApplication.CreateBuilder(args);

// register ClinicRepository with connection string from configuration
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    return new ClinicRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});

// register DoctorBranchCodeRepository the same way so DI can resolve controllers that depend on it
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = cfg.GetConnectionString("DefaultConnection") ?? cfg["ConnectionStrings:DefaultConnection"];
    return new DoctorBranchCodeRepository(conn ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));
});

// optional: register repository interfaces to the concrete instances (if you later inject interfaces)
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.DoctorBranchCode>>(sp => sp.GetRequiredService<DoctorBranchCodeRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Clinic>>(sp => sp.GetRequiredService<ClinicRepository>());

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