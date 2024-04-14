namespace AdventureLandSharp.Core.DependencyInjection;

public static class ServiceCollectionExtension {
    public static async Task<IServiceCollection> AddAdventureLandSharp(
        this IServiceCollection services,
        ApiConfiguration configuration) {
        services.AddSingleton(configuration);
        _api = new(configuration);
        services.AddSingleton(_api);

        _gameData = await _api.FetchGameDataAsync();
        _world = new(in _gameData);

        services.AddSingleton(_world);

        services.AddSingleton<SocketFactory>(provider => new(provider, in _gameData));

        return services;
    }
    private static Api _api = null!;
    private static GameData _gameData;
    private static World _world = null!;
}
