namespace AdventureLandSharp.Core.DependencyInjection;

public static class ServiceCollectionExtension
{
    private static Api _api = null!;
    private static GameData _gameData;
    private static World _world = null!;
    public static async Task<IServiceCollection> AddAdventureLandSharp(this IServiceCollection services,
        ApiConfiguration configuration)
    {
        services.AddSingleton(configuration);
        _api = new Api(configuration);
        services.AddSingleton(_api);

        _gameData = await _api.FetchGameDataAsync();
        _world = new World(in _gameData);

        services.AddSingleton(_world);

        services.AddSingleton<SocketFactory>(provider => new SocketFactory(provider, in _gameData));

        return services;
    }
}