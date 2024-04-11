using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using JsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace AdventureLandSharp.WebAPI;

public class NamedSystemTextJsonInputFormatter(
    string settingsName,
    JsonOptions options,
    ILogger<NamedSystemTextJsonInputFormatter> logger)
    : SystemTextJsonInputFormatter(options, logger)
{
    public string SettingsName { get; } = settingsName;
    public override bool CanRead(InputFormatterContext context) =>
        context.HttpContext.GetJsonSettingsName() == SettingsName && base.CanRead(context);
}

public class NamedSystemTextJsonOutputFormatter(
    string settingsName,
    JsonSerializerOptions jsonSerializerOptions) : SystemTextJsonOutputFormatter(jsonSerializerOptions)
{
    public string SettingsName { get; } = settingsName;
    public override bool CanWriteResult(OutputFormatterCanWriteContext context) =>
        context.HttpContext.GetJsonSettingsName() == SettingsName && base.CanWriteResult(context);
}

public class ConfigureMvcJsonOptions(
    string jsonSettingsName,
    IOptionsMonitor<JsonOptions> jsonOptions,
    ILoggerFactory loggerFactory) : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options) {
        JsonOptions opts = jsonOptions.Get(jsonSettingsName);
        NamedSystemTextJsonInputFormatter input = new(jsonSettingsName, opts, loggerFactory.CreateLogger<NamedSystemTextJsonInputFormatter>());  
        NamedSystemTextJsonOutputFormatter output = new(jsonSettingsName, opts.JsonSerializerOptions);   
        options.InputFormatters.Insert(0, input);
        options.OutputFormatters.Insert(0, output);
    }
}

public static class JsonExtensions {
    public static IMvcBuilder AddJsonOptions(this IMvcBuilder builder, string settingsName, Action<JsonOptions> configure) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(settingsName, configure);
        builder.Services.AddSingleton<IConfigureOptions<MvcOptions>>(sp =>{
            IOptionsMonitor<JsonOptions> options = sp.GetRequiredService<IOptionsMonitor<JsonOptions>>();
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new ConfigureMvcJsonOptions(settingsName, options, loggerFactory);
        });

        return builder;
    }

    public static string? GetJsonSettingsName(this HttpContext context) => 
        context.GetEndpoint()?.Metadata.GetMetadata<JsonSettingsNameAttribute>()?.Name;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class JsonSettingsNameAttribute(string name) : Attribute {
    public string Name { get; } = name;
}

