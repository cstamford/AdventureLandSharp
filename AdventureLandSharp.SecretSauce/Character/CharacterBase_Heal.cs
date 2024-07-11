using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Character;

public abstract partial class CharacterBase {
    protected virtual INode HealBuild() => new Selector(
        _consumeElixirCd.IfThenDo(() => Me.Equipment.Elixir == null, ConsumeElixir),
        _consumePotionsCd.ThenDo(ConsumePotion)
    );

    protected virtual void HealUpdate() {
        _healBt.Tick();
    }

    private readonly INode _healBt;
    private readonly Cooldown _consumeElixirCd = new(NetworkThrottleReadResponse);
    private readonly Cooldown _consumePotionsCd = new(TimeSpan.FromSeconds(2), NetworkSpamSlowCooldownDivisor);

    private Status ConsumeElixir() {
        int slotId = Cfg.Elixir == null ? -1 : Me.Inventory.FindSlotId(Cfg.Elixir);

        if (slotId != -1) {
            Socket.Emit<Outbound.Equip>(new(slotId));
            return Status.Success;
        }

        return Status.Fail;
    }

    private Status ConsumePotion() {
        int hpSlotId = Cfg.HealthPotion == null ? -1 : Me.Inventory.FindSlotId(Cfg.HealthPotion);
        int mpSlotId = Cfg.ManaPotion == null ? -1 : Me.Inventory.FindSlotId(Cfg.ManaPotion);

        int? equipSlotId = null;

        if (hpSlotId != -1 && Me.HealthPercent <= 25) {
            equipSlotId = hpSlotId;
        } else if (mpSlotId != -1 && Me.ManaPercent <= 25) {
            equipSlotId = mpSlotId;
        } else if (hpSlotId != -1 && Me.HealthPercent <= 50) {
            equipSlotId = hpSlotId;
        } else if (mpSlotId != -1 && Me.ManaPercent <= 50) {
            equipSlotId = mpSlotId;
        } else if (hpSlotId != -1 && Me.HealthPercent <= 75) {
            equipSlotId = hpSlotId;
        } else if (mpSlotId != -1 && Me.ManaPercent <= 75) {
            equipSlotId = mpSlotId;
        } else if (hpSlotId != -1 && Me.HealthMissing >= 500) {
            equipSlotId = hpSlotId;
        } else if (mpSlotId != -1 && Me.ManaMissing >= 250) {
            equipSlotId = mpSlotId;
        }

        if (equipSlotId != null) {
            Socket.Emit<Outbound.Equip>(new(equipSlotId.Value));
            return Status.Success;
        }

        string? useId = Cfg.ShouldUsePassiveRestore ?
            Me.ManaMissing > 0 ? "mp" :
            Me.HealthMissing > 0 ? "hp" :
            null : null;

         if (useId != null) {
            Socket.Emit<Outbound.Use>(new(useId));
            return Status.Success;
        }

        return Status.Fail;
    }
}
