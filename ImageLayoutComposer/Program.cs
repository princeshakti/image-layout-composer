using ImageLayoutComposer.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 80 * 1024 * 1024;
});

// CORS registered always, activated only in Development.
// In Production, replace AllowAnyOrigin with a specific allowed origin.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddSingleton<IImageComposerService, ImageComposerService>();
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors();
}

app.UseStaticFiles();
app.MapControllers();
app.Run();

public partial class Program { }
