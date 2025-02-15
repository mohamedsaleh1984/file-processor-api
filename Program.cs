using FileProcessorApi.Hubs;
using FileProcessorApi.Models;
using FileProcessorApi.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddHostedService<FileProcessingService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IBackgroundQueue<FileProcessingTask>, BackgroundQueue<FileProcessingTask>>();

builder.Services.AddCors(ops =>
{
    ops.AddPolicy("AllowReact", builder =>
            builder.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
});
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseRouting();

app.UseAuthorization();

app.UseCors("AllowReact");

app.UseEndpoints(static endpoints =>
{
    _ = endpoints.MapControllers();
    endpoints.MapHub<ProcessingHub>("/processingHub");
});
app.Run();  
