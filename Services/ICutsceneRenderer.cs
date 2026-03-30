namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Renders cutscenes to the console
/// </summary>
public interface ICutsceneRenderer
{
    /// <summary>
    /// Plays the given cutscene by displaying its text lines with appropriate formatting
    /// </summary>
    void PlayCutscene(Cutscene cutscene);
}
