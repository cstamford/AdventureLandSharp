using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;

namespace AdventureLandSharp.Interfaces;

public enum CharacterClass {
    Mage,
    Merchant,
    Paladin,
    Priest,
    Ranger,
    Rogue,
    Warrior
};

public interface ICharacter {
    public LocalPlayer Me { get; }
    public bool Update();
}

public delegate ICharacter CharacterFactory(
    World world,
    Socket socket,
    CharacterClass cls
);