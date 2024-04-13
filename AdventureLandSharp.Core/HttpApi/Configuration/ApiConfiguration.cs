namespace AdventureLandSharp.Core.HttpApi.Configuration;

public class ApiConfiguration
{
    public ApiAddress? Address { get; set; }
    public ApiCredentials Credentials { get; set; }
}

public class ApiAddress
{
    public bool Https { get; set; }
    public string? HostName { get; set; }
    public int? Port { get; set; }

    public string Address => $"{Protocol}://{HostName}{PortString}";

    private string Protocol => $"http{(Https ? "s" : "")}";
    private string PortString => Port is not null ? $":{Port}" : "";
}

public readonly record struct ApiCredentials(string Email, string Password);