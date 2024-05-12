using System.Text.Json.Serialization;
using AdventureLandSharp.Core;
using AdventureLandSharp.Core.HttpApi;
using AdventureLandSharp.Core.Util;
using AdventureLandSharp.WebAPI;

GameData gameData = await Api.FetchGameDataAsync(new("http://localhost:8083"));
World world = new(gameData, Utils.LoadSmapData());

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(world);
builder.Services.AddControllers()
    .AddJsonOptions(opts => {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonOpts.Default.PropertyNamingPolicy;
        opts.JsonSerializerOptions.WriteIndented = JsonOpts.Default.WriteIndented;
        foreach (JsonConverter converter in JsonOpts.Default.Converters) {
            opts.JsonSerializerOptions.Converters.Add(converter);
        }
    })
    .AddJsonOptions("condensed", opts => {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonOpts.Condensed.PropertyNamingPolicy;
        opts.JsonSerializerOptions.WriteIndented = JsonOpts.Condensed.WriteIndented;
        foreach (JsonConverter converter in JsonOpts.Condensed.Converters) {
            opts.JsonSerializerOptions.Converters.Add(converter);
        }
    });
builder.Services.AddCors(opts => {
    opts.AddPolicy("Any", x => { x
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Any");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
