using Microsoft.Extensions.Caching.Memory;
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
        readonly IUsersProfileApi _usersProfileApi;
        readonly IMemoryCache _memoryCache;

        readonly string[] scopes = new[]{ "playlist-read-private", "user-modify-playback-state", "user-read-playback-state" };
        const string lucasfogliariniId = "12145833562";

        public SpotifyPlayer(IUserAccountsService userAccountsService,
                             IPlayerApi playerApi,
                             IPlaylistsApi playlistsApi,
                             ITracksApi tracksApi,
                             IUsersProfileApi usersProfileApi,
                             IMemoryCache memoryCache)
        {
            _userAccountsService = userAccountsService;
            _playerApi = playerApi;
            _playlistsApi = playlistsApi;
            _tracksApi = tracksApi;
            _usersProfileApi = usersProfileApi;
            _memoryCache = memoryCache;
        }

        public async Task<string?> Login(string? code)
        {
            if (code == null)
            {
                string state = Guid.NewGuid().ToString("N");
                var url = _userAccountsService.AuthorizeUrl(state, scopes);
                return url;
            }
            var token = await _userAccountsService.RequestAccessRefreshToken(code);
            _memoryCache.Set(nameof(BearerAccessToken.AccessToken), token.AccessToken);
            return null;
        }
        public async Task PlayTracks(string trackId)
        {
            var devices = await _playerApi.GetDevices(GetAccessToken());
            if (!devices.Any())
            {
                throw new Exception("Entre em algum dispositivo Spotify!");
            }

            await _playerApi.PlayTracks(trackId, GetAccessToken(), deviceId: devices[0].Id);
        }
        public async Task<IEnumerable<TrackRate>> TracksByPopularity(string? playlistName = null, bool desc = true)
        {
            var tracks = playlistName == null ? null : await GetPlaylistTracks(playlistName);
            tracks ??= await GetCurrentUsersTracks(playlistName);

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
            tracks ??= await GetCurrentUsersTracks(playlistName);

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
            var currentUsersTracks = await GetCurrentUsersTracks();
            var tracksRate = new List<TrackRate>();
            foreach (var track in currentUsersTracks)
            {
                var trackAudioFeatures = await _tracksApi.GetTrackAudioFeatures(track.Track.Id);
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
            var publicTracks = await GetCurrentUsersTracks();
            var tracksRate = new List<TrackRate>();
            foreach (var track in publicTracks)
            {
                var trackAudioFeatures = await _tracksApi.GetTrackAudioFeatures(track.Track.Id);
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
        public async Task<IEnumerable<PlaylistTrack>> GetCurrentUsersTracks(string? playlistName = null)
        {
            //dont works yet
            //var currentUsersPlaylists = await _playlistsApi.GetCurrentUsersPlaylists(accessToken: GetAccessToken());
            var user = await _usersProfileApi.GetCurrentUsersProfile(GetAccessToken());
            var currentUsersPlaylists = await _playlistsApi.GetPlaylists(user.Id, GetAccessToken(), limit: 50);
            var currentUsersTracks = new List<PlaylistTrack>();
            IEnumerable<PlaylistSimplified> publicPlaylistsSimplified = currentUsersPlaylists.Items;
            if (playlistName != null)
            {
                publicPlaylistsSimplified = currentUsersPlaylists.Items.Where(e => e.Name.Contains(playlistName));
            }

            foreach (var playlist in publicPlaylistsSimplified)
            {
                PlaylistPaged playlistPaged = await _playlistsApi.GetTracks(playlist.Id, GetAccessToken());
                currentUsersTracks.AddRange(playlistPaged.Items);
            }
            return currentUsersTracks;
        }
        public async Task<IEnumerable<PlaylistTrack>> GetPlaylistTracks(string playlistId)
        {
            PlaylistPaged playlistPaged = new();
            try
            {
                var playlist = await _playlistsApi.GetPlaylist(playlistId);
                playlistPaged = await _playlistsApi.GetTracks(playlist.Id);
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
        private string? GetAccessToken() => _memoryCache.Get(nameof(BearerAccessToken.AccessToken)).ToString();
    }
}
