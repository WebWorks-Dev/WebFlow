using System;
using System.Reflection;
using System.Text.Json.Serialization;
using AspNetCore.ClaimsValueProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebFlow.Caching;
using WebFlow.Authorization;
using WebFlow.Models;
using WebFlow.Passwords;
using WebFlowTest.Exensions;

var jwtConfig = new JwtConfig
{
    // Initialize your JwtConfig properties here
    Issuer = "your_issuer",
    Audience = "your_audience",
    Key = "your_key",
    Duration = DateTime.UtcNow.AddMinutes(120)
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.RegisterCachingService(Assembly.GetExecutingAssembly(), "127.0.0.1:6379");
builder.Services.RegisterAuthorizationService(Assembly.GetExecutingAssembly(), jwtConfig);
builder.Services.RegisterPasswordHashing();

builder.Services.RegisterDataServices();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMvc(options =>
{
    options.AddClaimsValueProvider();
});

WebApplication app = builder.Build();
app.UseCors("CORSPolicy");
app.UseRouting();
app.MapControllers();

app.UseAuthorization();
app.RegisterAuthorizationMiddlewares(AuthorizationType.Jwt);
/*using (var serviceScope = app.Services.CreateScope())
{
    var context = serviceScope.ServiceProvider.GetRequiredService<EntityFrameworkContext>();
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
}*/

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = true;
    });
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat System");
    });
}

app.Run();