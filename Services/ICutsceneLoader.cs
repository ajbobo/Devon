namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Loads cutscenes from a JSON source
/// </summary>
public interface ICutsceneLoader
{
    /// <summary>
    /// Loads all cutscenes from the embedded resource
    /// </summary>
    Task<IReadOnlyDictionary<string, Cutscene>> LoadCutscenesAsync();
}
