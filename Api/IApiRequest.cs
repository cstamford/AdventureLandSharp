using System.Text.Json.Serialization;

namespace AdventureLandSharp.Api;

public interface IApiRequest {
    [JsonIgnore] string Method { get; }
}
