using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Helpers;

namespace AdventureLandSharp.Test;

[TestClass]
public static class InitWorld {
    public static World World { get; private set; } = default!;

    [AssemblyInitialize()]
    public static async Task Init(TestContext testContext) {
        GameData data = await Api.FetchGameDataAsync(Utils.ApiAddress);
        World = new(data);
    }

    [AssemblyCleanup]
    public static void TearDown() {
    }
}