using System.Text.Json.Serialization;
using Dalamud.Utility;

namespace SproutTracker;

public class Character {
    [JsonInclude]
    [JsonPropertyName("contentId")]
    public ulong ContentId = Services.PlayerState.ContentId;

    [JsonInclude]
    [JsonPropertyName("name")]
    public string Name = Services.ObjectTable.LocalPlayer!.Name.TextValue;

    [JsonInclude]
    [JsonPropertyName("world")]
    public string World = Services.ObjectTable.LocalPlayer.HomeWorld.Value.Name.ExtractText();
}
