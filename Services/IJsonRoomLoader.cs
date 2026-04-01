namespace Devon.Services;

using Devon.Models;

/// <summary>
/// Loads room definitions from a JSON source (embedded resource)
/// </summary>
public interface IJsonRoomLoader
{
    /// <summary>
    /// Loads all rooms from the embedded resource
    /// </summary>
    Task<IReadOnlyDictionary<string, Room>> LoadRoomsAsync();
}
