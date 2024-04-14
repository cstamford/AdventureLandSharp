namespace AdventureLandSharp.Core.SocketApi;

public class SocketFactory {
    public SocketFactory(IServiceProvider provider, ref readonly GameData gameData) {
        _provider = provider;
        _gameData = gameData;
    }

    public Socket CreateSocket(ConnectionSettings settings) {
        ILogger<Socket>? logger = _provider.GetService<ILogger<Socket>>()!;
        ILogger<SocketConnection>? connectionLogger = _provider.GetService<ILogger<SocketConnection>>()!;
        SocketConnection connection = new SocketConnection(connectionLogger, settings);
        return new(in _gameData, connection, logger);
    }
    private readonly GameData _gameData;
    private readonly IServiceProvider _provider;
}
