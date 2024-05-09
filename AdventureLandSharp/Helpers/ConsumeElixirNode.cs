using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.Helpers;

public class ConsumeElixirNode(
    Func<(Socket socket, LocalPlayer me)> fnGetSelf,
    string? elixir = null
) : INode {
    public Status Tick() {
        (Socket socket, LocalPlayer me) = fnGetSelf();

        if (_cd.Ready) {
            int slotId = elixir == null ? -1 : me.Inventory.FindSlotId(elixir);

            if (slotId != -1) {
                socket.Emit<Outbound.Equip>(new(slotId));
                _cd.Restart();
                return Status.Success;
            }
        }

        return Status.Fail;
    }

    private readonly Cooldown _cd = new(TimeSpan.FromSeconds(2));
}

