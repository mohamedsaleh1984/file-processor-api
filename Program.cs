using FileProcessorApi.Hubs;
using FileProcessorApi.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IScopedProcessingService, ScopedProcessingService>();
builder.Services.AddSingleton<FileProcessingService>();
builder.Services.AddHostedService<FileProcessingService>();
builder.Services.AddCors(ops =>
{
    ops.AddPolicy("AllowReact", builder =>
            builder.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());

});

builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.UseCors("AllowReact");
app.UseEndpoints(static endpoints =>
{
    _ = endpoints.MapControllers();
    endpoints.MapHub<ProcessingHub>("/processingHub");
});
app.Run();  
