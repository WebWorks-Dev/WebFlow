using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebFlowTest.Exensions;

public static class Registration
{
    public static void RegisterDataServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContextFactory<EntityFrameworkContext>(options =>
        {
            options.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=root;Database=YOUR_DB;Include Error Detail=true");
            //options.EnableSensitiveDataLogging(); // Optional, for debugging
        });
    }
}