using System.Text.Json;
using AdventureLandSharp.SecretSauce.Character;
using AdventureLandSharp.SecretSauce.Strategy;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.SocketApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.Helpers;
using AdventureLandSharp.Interfaces;
using AdventureLandSharp.Utility;

namespace AdventureLandSharp.SecretSauce.Classes;

public class Merchant(World world, Socket socket, CharacterConfig config) : CharacterBase(world, socket, config) {
    public override CharacterClass Class => CharacterClass.Merchant;
    public override bool InCombat => false;

    protected override INode ClassBuild() => new Selector(
        new If(() => CanFish, Skill("fishing", Fish)),
        new If(() => CanMine, Skill("mining", Mine)),
        new If(() => CanMerchantsLuck, Skill("mluck", MerchantsLuck))
    );

    protected override void ClassUpdate() {
        if (Me.Actions.Fishing.HasValue) {
            _fishingEnds = DateTimeOffset.UtcNow.Add(Me.Actions.Fishing.Value.Duration);
        }

        if (Me.Actions.Mining.HasValue) {
            _miningEnds = DateTimeOffset.UtcNow.Add(Me.Actions.Mining.Value.Duration);
        }

        base.ClassUpdate();
    }

    protected override void OnSocket() {
        base.OnSocket();

        Socket.OnGameResponse += evt => {
            if (evt.TryGetProperty("response", out JsonElement response) && evt.TryGetProperty("place", out JsonElement place)) {
                string responseStr = response.GetString()!;
                string placeStr = place.GetString()!;

                if (responseStr == "data" && placeStr == "fishing") {
                    _fishingEnds = DateTimeOffset.UtcNow.AddMilliseconds(500);
                } else if (responseStr == "data" && placeStr == "mining") {
                    _miningEnds = DateTimeOffset.UtcNow.AddMilliseconds(500);
                }
            }
        };
    }

    protected override void OnStrategy() {
        base.OnStrategy();
        AvailableStrategies.Add(new DynamicStrategy(() => UrgentBuffTarget.HasValue, () => UrgentBuffTarget!.Value));
        AvailableStrategies.Add(new DynamicStrategy(() => WantsToFish, () => _fishingSpot));
        AvailableStrategies.Add(new DynamicStrategy(() => WantsToMine, () => _miningSpot));
        AvailableStrategies.Add(new HighValue_PhoenixScoutStrategy(this));
        AvailableStrategies.Add(new HighValue_RespawnMobScout(this, "mvampire", TimeSpan.FromSeconds(1080), [..Utils.GetMapLocationsForSpawn(World, "cave", "mvampire")]));
        AvailableStrategies.Add(new HighValue_RespawnMobScout(this, "fvampire", TimeSpan.FromSeconds(1440), [..Utils.GetMapLocationsForSpawn(World, "fvampire")]));
        AvailableStrategies.Add(new HighValue_RespawnMobScout(this, "greenjr", TimeSpan.FromSeconds(51840), [Utils.GetMapLocationForSpawn(World, "greenjr")]));
        AvailableStrategies.Add(new HighValue_RespawnMobScout(this, "jr", TimeSpan.FromSeconds(25920), [Utils.GetMapLocationForSpawn(World, "jr")]));
        AvailableStrategies.Add(new FarmLocationStrategy(new(World.GetMap("cave"), new(-397, -1239))));
    }

    protected override CharacterLoadout DesiredLoadout =>
        WantsToFish && MyLoc.Equivalent(_fishingSpot) ? default(CharacterLoadout) with { MainHand = "rod" } :
        WantsToMine && MyLoc.Equivalent(_miningSpot) ? default(CharacterLoadout) with { MainHand = "pickaxe" } :
        default(CharacterLoadout) with { MainHand = "broom" };

    private MapLocation? UrgentBuffTarget => CharacterStatuses
        .FirstOrNull(x => 
            !x.Value.StatusEffects.MLuck.HasValue ||
            x.Value.StatusEffects.MLuck.Value.Owner != Me.Id ||
            (MyLoc.Equivalent(x.Value.Location, 1000) && x.Value.StatusEffects.MLuck.Value.Duration <= _merchantRecastHard))
        ?.Value.Location;

    private bool IsFishing => DateTimeOffset.UtcNow < _fishingEnds;
    private bool IsMining => DateTimeOffset.UtcNow < _miningEnds;

    private bool HasFishingEquipment => Equipment.MainHand == "rod" || Items.Any(x => x.Name == "rod");
    private bool HasMiningEquipment => Equipment.MainHand == "pickaxe" || Items.Any(x => x.Name == "pickaxe");

    private bool CanFish => MyLoc.Equivalent(_fishingSpot) && Equipment.MainHand == "rod" && !IsFishing;
    private bool CanMine => MyLoc.Equivalent(_miningSpot) && Equipment.MainHand == "pickaxe" && !IsMining;
    private bool CanMerchantsLuck => MerchantLuckTarget.HasValue;

    private bool WantsToFish => (FishingCd.Ready || IsFishing) && HasFishingEquipment;
    private bool WantsToMine => (MiningCd.Ready || IsMining) && HasMiningEquipment;

    private void Fish() {
        Socket.Emit<Outbound.UseSkill>(new("fishing"));
        Log.Info($"We're fishing!");
        _fishingEnds = DateTimeOffset.UtcNow.AddMilliseconds(500);
    }

    private void Mine() {
        Socket.Emit<Outbound.UseSkill>(new("mining"));
        Log.Info($"We're mining!");
        _miningEnds = DateTimeOffset.UtcNow.AddMilliseconds(500);
    }

    private void MerchantsLuck() {
        Socket.Emit<Outbound.UseSkillOnId>(new("mluck", MerchantLuckTarget!.Value.Id));
    }

    private Cooldown FishingCd => Cooldown("fishing");
    private Cooldown MiningCd => Cooldown("mining");
    private CachedPlayer? MerchantLuckTarget => Players
        .Where(x => x.Distance <= 320 && (
            !x.Player.StatusEffects.MLuck.HasValue ||
            (x.Player.StatusEffects.MLuck.Value.Owner != Me.Id && PartyPlayers.Any(y => y.Player.Name == x.Player.Name)) ||
            (x.Player.StatusEffects.MLuck.Value.Owner == Me.Id && x.Player.StatusEffects.MLuck.Value.Duration <= _merchantRecastSoft)))
        .FirstOrNull();

    private readonly MapLocation _fishingSpot = world.GetMap("main").FishingSpot!.Value;
    private readonly MapLocation _miningSpot = world.GetMap("tunnel").MiningSpot!.Value;

    private DateTimeOffset _fishingEnds = DateTimeOffset.UtcNow;
    private DateTimeOffset _miningEnds = DateTimeOffset.UtcNow;

    private readonly TimeSpan _merchantRecastHard = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _merchantRecastSoft = TimeSpan.FromMinutes(55);
}
