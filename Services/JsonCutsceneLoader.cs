namespace Devon.Services;

using Devon.Models;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Loads cutscene definitions from an embedded JSON resource
/// </summary>
public class JsonCutsceneLoader : ICutsceneLoader
{
    private readonly string _resourceName;

    public JsonCutsceneLoader(string? resourceName = null)
    {
        _resourceName = resourceName ?? "Devon.cutscenes.json";
    }

    public async Task<IReadOnlyDictionary<string, Cutscene>> LoadCutscenesAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        var fullResourceName = resourceNames.FirstOrDefault(r => r.EndsWith("cutscenes.json", StringComparison.OrdinalIgnoreCase));
        if (fullResourceName == null)
            throw new FileNotFoundException($"Embedded resource 'cutscenes.json' not found. Available: {string.Join(", ", resourceNames)}");

        using var stream = assembly.GetManifestResourceStream(fullResourceName) ?? throw new InvalidOperationException($"Could not open resource stream for {fullResourceName}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var doc = await JsonSerializer.DeserializeAsync<JsonDocument>(stream);
        var cutscenesDict = new Dictionary<string, Cutscene>(StringComparer.OrdinalIgnoreCase);

        if (!doc.RootElement.TryGetProperty("cutscenes", out JsonElement cutscenesElement))
            throw new InvalidOperationException("JSON missing 'cutscenes' array");

        foreach (var cutsceneElem in cutscenesElement.EnumerateArray())
        {
            var cutscene = ParseCutscene(cutsceneElem);
            cutscenesDict[cutscene.Name] = cutscene;
        }

        return cutscenesDict;
    }

    private Cutscene ParseCutscene(JsonElement cutsceneElem)
    {
        var cutscene = new Cutscene
        {
            Name = cutsceneElem.GetProperty("name").GetString() ?? throw new InvalidOperationException("Cutscene missing name")
        };

        if (cutsceneElem.TryGetProperty("text", out JsonElement textElem) && textElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var textItem in textElem.EnumerateArray())
            {
                var cutsceneText = new CutsceneText
                {
                    Text = textItem.GetProperty("text").GetString() ?? "",
                    Color = textItem.TryGetProperty("color", out JsonElement colorElem) ? colorElem.GetString() : null,
                    Wait = textItem.TryGetProperty("wait", out JsonElement waitElem) && waitElem.ValueKind == JsonValueKind.True,
                    Clear = textItem.TryGetProperty("clear", out JsonElement clearElem) && clearElem.ValueKind == JsonValueKind.True
                };
                cutscene.Text.Add(cutsceneText);
            }
        }

        return cutscene;
    }
}
