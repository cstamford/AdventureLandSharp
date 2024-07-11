using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    protected abstract INode ClassBuild();

    protected virtual void ClassUpdate() {
        _classBt.Tick();
    }

    private readonly INode _classBt;
}
