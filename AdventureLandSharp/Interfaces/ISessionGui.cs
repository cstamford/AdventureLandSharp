using AdventureLandSharp.Core;

namespace AdventureLandSharp.Interfaces;

public interface ISessionGui : IDisposable {
    public bool Update();
}

public delegate ISessionGui GuiFactory(
    World world,
    ICharacter character
);
