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
    public Socket Socket { get; }
    public CharacterClass Class { get; }
    public LocalPlayer Entity { get; }
    public MapLocation EntityLocation { get; }
    public bool Update();
}

public delegate ICharacter CharacterFactory(
    World world,
    Socket socket,
    CharacterClass cls
);
