using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.Helpers;

public class ConsumePotionsNode(
    Func<(Socket socket, LocalPlayer me)> fnGetSelf,
    string? healthPot = "hpot0",
    string? manaPot = "mpot0"
) : INode {
    public Status Tick() {
        (Socket socket, LocalPlayer me) = fnGetSelf();

        if (_cd.Ready) {
            int hpSlotId = healthPot == null ? -1 : me.Inventory.FindSlotId(healthPot);
            int mpSlotId = manaPot == null ? -1 : me.Inventory.FindSlotId(manaPot);

            int? equipSlotId = null;
            string? useId = null;

            if (me.HealthPercent < 65 && hpSlotId != -1) {
                equipSlotId = hpSlotId;
            } else if (me.ManaPercent < 65 && mpSlotId != -1) {
                equipSlotId = mpSlotId;
            } else if (me.ManaPercent < 90) {
                useId = "mp";
            } else if (me.HealthPercent < 90) {
                useId = "hp";
            } else if (me.ManaPercent < 100) {
                useId = "mp";
            } else if (me.HealthPercent < 100) {
                useId = "hp";
            }

            if (equipSlotId != null) {
                socket.Emit<Outbound.Equip>(new(equipSlotId.Value));
                _cd.Duration = TimeSpan.FromSeconds(2);
                _cd.Restart();
                return Status.Success;
            }

            if (useId != null) {
                socket.Emit<Outbound.Use>(new(useId));
                _cd.Duration = TimeSpan.FromSeconds(4);
                _cd.Restart();
                return Status.Success;
            }
        }

        return Status.Fail;
    }

    private readonly Cooldown _cd = new(TimeSpan.FromSeconds(4));
}

