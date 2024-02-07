using SocketIOClient;

namespace AdventureLandSharp.Util;

public static class Extensions {
    public static void OnSafe(this SocketIOClient.SocketIO client, string eventName, Action<SocketIOResponse> callback) {
        client.On(eventName, response => {
            try {
                callback(response);
            } catch (Exception ex) {
                Console.WriteLine($"Exception occurred during socket event '{eventName}': {ex}");
            }
        });
    }

    public static void OnSafe(this SocketIOClient.SocketIO client, string eventName, Func<SocketIOResponse, Task> callback) {
        client.On(eventName, async response => {
            try {
                await callback(response);
            } catch (Exception ex) {
                Console.WriteLine($"Exception occurred during socket event '{eventName}': {ex}");
            }
        });
    }

    public static Task EmitSafe(this SocketIOClient.SocketIO client, string eventName, object data) {
        // TODO: Rate limit!
        return client.EmitAsync(eventName, data);
    }
}
