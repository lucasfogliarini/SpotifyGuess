using SpotifyApi.NetCore;

namespace SpotifyGuess
{
    public interface ISpotifyPlayer
    {
        Task<string> Login(string? code);
        Task PlayTracks(string trackId);
        Task CreatePlaylist(string name, IEnumerable<string> trackUris);
        Task<IEnumerable<PlaylistTrack>> GetCurrentUsersTracks(string? playlistName = null);
    }
}
