IConfigurationRoot configuration = new ConfigurationBuilder()
    .AddJsonFile("AppSettings.json")
    .AddEnvironmentVariables()
    .Build();

ApiConfiguration? apiConfiguration = configuration.GetSection("AdventureLand").GetSection("Api").Get<ApiConfiguration>();

if (apiConfiguration is null) return;

ServiceCollection services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole());

await services.AddAdventureLandSharp(apiConfiguration);

services.AddScoped<Bot>();

ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();

Bot bot = scope.ServiceProvider.GetService<Bot>()!;

await bot.Run();
