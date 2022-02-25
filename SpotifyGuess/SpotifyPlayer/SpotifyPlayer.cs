using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Authorization;

namespace SpotifyGuess
{
    internal class SpotifyPlayer : ISpotifyPlayer
    {
        readonly IUserAccountsService _userAccountsService;
        readonly IPlayerApi _playerApi;
        readonly IPlaylistsApi _playlistsApi;
        readonly ITracksApi _tracksApi;

        readonly string[] scopes = new[]{ "user-modify-playback-state", "user-read-playback-state" };
        const string lucasfogliariniId = "12145833562";

        public SpotifyPlayer(IUserAccountsService userAccountsService,
                             IPlayerApi playerApi,
                             IPlaylistsApi playlistsApi,
                             ITracksApi tracksApi)
        {
            _userAccountsService = userAccountsService;
            _playerApi = playerApi;
            _playlistsApi = playlistsApi;
            _tracksApi = tracksApi;
        }

        public async Task PlayTracks(string trackId)
        {
            //string state = Guid.NewGuid().ToString("N");
            //var url = _userAccountsService.AuthorizeUrl(state, scopes);

            //var token = await _userAccountsService.RequestAccessRefreshToken(code);
            //var devices = await _playerApi.GetDevices(token.AccessToken);

            //await _playerApi.PlayTracks(trackId, token.AccessToken, deviceId: devices[0].Id);
        }
        public async Task<IEnumerable<TrackRate>> TracksByPopularity(string? playlistName = null, bool desc = true)
        {
            var tracks = playlistName == null ? null : await GetPlaylistTracks(playlistName);
            tracks ??= await GetPublicTracks(playlistName);

            var tracksRate = tracks.Select(e => new TrackRate
            {
                Id = e.Track.Id,
                Name = e.Track.Name,
                Artists = string.Join(',', e.Track.Artists.Select(e => e.Name)),
                Rate = e.Track.Popularity
            });
            return Order(tracksRate, desc);
        }
        public async Task<IEnumerable<TrackRate>> TracksByAge(string? playlistName = null, bool desc = true)
        {
            var tracks = playlistName == null ? null : await GetPlaylistTracks(playlistName);
            tracks ??= await GetPublicTracks(playlistName);

            var tracksRate = tracks.Select(e => new TrackRate
            {
                Id = e.Track.Id,
                Name = e.Track.Name,
                Artists = string.Join(',', e.Track.Artists.Select(e=>e.Name)),
                Rate = int.Parse(e.Track.Album.ReleaseDate[..4])
            });
            return Order(tracksRate, desc);
        }
        public async Task<IEnumerable<TrackRate>> TracksByDanceability(bool desc = true)
        {
            var publicTracks = await GetPublicTracks();
            var tracksRate = new List<TrackRate>();
            foreach (var track in publicTracks)
            {
                var trackAudioFeatures = await TracksApi.GetTrackAudioFeatures(track.Track.Id);
                tracksRate.Add(new TrackRate
                {
                    Id = track.Track.Id,
                    Name = track.Track.Name,
                    Artists = string.Join(',', track.Track.Artists.Select(e => e.Name)),
                    Rate = trackAudioFeatures.Danceability
                });
            }
            return Order(tracksRate, desc);
        }
        public async Task<IEnumerable<TrackRate>> TracksByEnergy(bool desc = true)
        {
            var publicTracks = await GetPublicTracks();
            var tracksRate = new List<TrackRate>();
            foreach (var track in publicTracks)
            {
                var trackAudioFeatures = await TracksApi.GetTrackAudioFeatures(track.Track.Id);
                tracksRate.Add(new TrackRate
                {
                    Id = track.Track.Id,
                    Name = track.Track.Name,
                    Artists = string.Join(',', track.Track.Artists.Select(e => e.Name)),
                    Rate = trackAudioFeatures.Energy
                });
            }
            return Order(tracksRate, desc);
        }
        public async Task<IEnumerable<PlaylistTrack>> GetPublicTracks(string? playlistName = null)
        {
            var publicPlaylists = await PlaylistsApi.GetPlaylists(UserId);
            var publicTracks = new List<PlaylistTrack>();
            IEnumerable<PlaylistSimplified> publicPlaylistsSimplified = publicPlaylists.Items;
            if (playlistName != null)
            {
                publicPlaylistsSimplified = publicPlaylists.Items.Where(e => e.Name.Contains(playlistName));
            }

            foreach (var playlist in publicPlaylistsSimplified)
            {
                PlaylistPaged playlistPaged = await PlaylistsApi.GetTracks(playlist.Id);
                publicTracks.AddRange(playlistPaged.Items);
            }
            return publicTracks;
        }
        public async Task<IEnumerable<PlaylistTrack>> GetPlaylistTracks(string playlistId)
        {
            PlaylistPaged playlistPaged = new();
            try
            {
                var playlist = await PlaylistsApi.GetPlaylist(playlistId);
                playlistPaged = await PlaylistsApi.GetTracks(playlist.Id);
                return playlistPaged.Items;
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static IEnumerable<TrackRate> Order(IEnumerable<TrackRate> tracksRate, bool desc = true)
        {
            return desc ? tracksRate.OrderByDescending(e => e.Rate) : tracksRate.OrderBy(e=>e.Rate);
        }
    }
}
