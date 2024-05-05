using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp.Interfaces;

public interface ISession : IDisposable {
    public ConnectionSettings Settings { get; }
    public void EnterUpdateLoop();
}

public delegate ISession SessionFactory(
    World world,
    ConnectionSettings settings,
    CharacterFactory characterFactory,
    GuiFactory? guiFactory
);

