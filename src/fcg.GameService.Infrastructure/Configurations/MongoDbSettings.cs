using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace fcg.GameService.Infrastructure.Configurations;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

public static class MongoDbService
{
    public static IServiceCollection AddMongoDBService(this IServiceCollection services, IConfiguration configuration)
    {
        MongoDbSettings mongoDbSettings = new();
        configuration.GetSection(nameof(MongoDbSettings)).Bind(mongoDbSettings);

        services.AddHealthChecks()
            .AddMongoDb(
                mongodbConnectionString: mongoDbSettings!.ConnectionString,
                name: "mongodb",
                timeout: TimeSpan.FromSeconds(5),
                tags: ["db", "mongo"]
            );

        services.Configure<MongoDbSettings>(
            configuration.GetSection(nameof(MongoDbSettings))
        );

        services.AddSingleton<IMongoClient>(sp =>
        {
            MongoDbSettings mongoDbSettings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            MongoClientSettings settings = MongoClientSettings.FromConnectionString(mongoDbSettings.ConnectionString);

            settings.ClusterConfigurator = cb =>
            {
                cb.Subscribe(new DiagnosticsActivityEventSubscriber());
            };

            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            settings.ConnectTimeout = TimeSpan.FromSeconds(5);

            return new MongoClient(settings);
        });

        services.AddSingleton(sp =>
        {
            IMongoClient client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoDbSettings.DatabaseName);
        });

        return services;
    }
}
