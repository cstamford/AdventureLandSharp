var configuration = new ConfigurationBuilder()
    .AddJsonFile("AppSettings.json")
    .AddEnvironmentVariables()
    .Build();

var apiConfiguration = configuration.GetSection("AdventureLand").GetSection("Api").Get<ApiConfiguration>();

if (apiConfiguration is null) return;

var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole());

await services.AddAdventureLandSharp(apiConfiguration);

services.AddScoped<Bot>();

var provider = services.BuildServiceProvider();
await using var scope = provider.CreateAsyncScope();

var bot = scope.ServiceProvider.GetService<Bot>()!;

await bot.Run();