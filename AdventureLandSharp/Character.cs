namespace AdventureLandSharp;

public enum CharacterClass {
    Mage,
    Merchant,
    Paladin,
    Priest,
    Ranger,
    Rogue,
    Warrior
}

public interface ICharacter {
    public Task<bool> Update(float dt);
}

public interface ICharacterFactory {
    public ICharacter Create(CharacterClass characterClass, World world, Socket socket);
}
