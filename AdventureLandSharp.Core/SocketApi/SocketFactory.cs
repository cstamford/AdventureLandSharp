namespace AdventureLandSharp.Core.SocketApi;

public class SocketFactory
{
    private readonly GameData _gameData;
    private readonly IServiceProvider _provider;

    public SocketFactory(IServiceProvider provider, ref readonly GameData gameData)
    {
        _provider = provider;
        _gameData = gameData;
    }

    public Socket CreateSocket(ConnectionSettings settings)
    {
        var logger = _provider.GetService<ILogger<Socket>>()!;
        var connectionLogger = _provider.GetService<ILogger<SocketConnection>>()!;
        var connection = new SocketConnection(connectionLogger, settings);
        return new Socket(in _gameData, connection, logger);
    }
}