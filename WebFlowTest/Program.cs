using System;
using System.Reflection;
using System.Text.Json.Serialization;
using AspNetCore.ClaimsValueProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebFlow.Caching;
using WebFlow.Authorization;
using WebFlow.Email;
using WebFlow.Models;
using WebFlow.Passwords;
using WebFlowTest;
using WebFlowTest.Exensions;

var jwtConfig = new JwtConfig
{
    // Initialize your JwtConfig properties here
    Issuer = "your_issuer",
    Audience = "your_audience",
    Key = "abcdefghijklmnoprsquvxyz123456789",
    Duration = DateTime.UtcNow.AddMinutes(120)
};

Assembly executingAssembly = Assembly.GetExecutingAssembly();
var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterCachingService(executingAssembly, "127.0.0.1:6379");

builder.Services.RegisterAuthorizationService(executingAssembly, jwtConfig);
builder.Services.UseEmailVerification(executingAssembly);

builder.Services.RegisterPasswordHashing();
builder.Services.RegisterEmailService("smtp.gmail.com:587", "YOUR_EMAIL", "YOUR_PASSWORD");
builder.Services.UseRecaptcha("YOUR_RECAPTCHA_KEY");

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