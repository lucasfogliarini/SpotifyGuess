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

        readonly string[] scopes = new[]{ "playlist-read-private", "playlist-modify-public", "playlist-read-collaborative", "playlist-modify-private", "user-modify-playback-state", "user-read-playback-state" };
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
        public async Task<IEnumerable<TrackRate>> TracksByDanceability(IEnumerable<PlaylistTrack> tracks, bool desc = true)
        {
            var tracksRate = new List<TrackRate>();
            foreach (var track in tracks)
            {
                var trackAudioFeatures = await _tracksApi.GetTrackAudioFeatures(track.Track.Id);
                tracksRate.Add(new TrackRate
                {
                    Id = track.Track.Id,
                    Name = track.Track.Name,
                    Artists = string.Join(',', track.Track.Artists.Select(e => e.Name)),
                    Danceability = trackAudioFeatures.Danceability
                });
            }
            return Order(tracksRate, desc);
        }
        public async Task<IEnumerable<TrackRate>> TracksByEnergy(IEnumerable<PlaylistTrack> tracks, bool desc = true)
        {
            var tracksRate = new List<TrackRate>();
            foreach (var track in tracks)
            {
                var trackAudioFeatures = await _tracksApi.GetTrackAudioFeatures(track.Track.Id);
                tracksRate.Add(new TrackRate
                {
                    Id = track.Track.Id,
                    Name = track.Track.Name,
                    Artists = string.Join(',', track.Track.Artists.Select(e => e.Name)),
                    Energy = trackAudioFeatures.Energy
                });
            }
            return Order(tracksRate, desc);
        }
        public async Task<IEnumerable<PlaylistTrack>> GetCurrentUsersTracks(string? playlistName = null)
        {
            //dont works yet
            //var currentUsersPlaylists = await _playlistsApi.GetCurrentUsersPlaylists(accessToken: GetAccessToken());
            var user = await _usersProfileApi.GetCurrentUsersProfile(GetAccessToken());
            var currentUsersTracks = new List<PlaylistTrack>();
            int offset = 0;
            bool getMore = true;
            while (getMore)
            {
                int limit = 50;
                var currentUsersPlaylists = await _playlistsApi.GetPlaylists<PlaylistsSearchResult>(user.Id, GetAccessToken(), limit: limit, offset: offset);
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
                offset += limit;
                getMore = currentUsersPlaylists.Total > offset;
            }
            
            return currentUsersTracks;
        }

        public async Task CreatePlaylist(string name, IEnumerable<string> trackUris)
        {
            var user = await _usersProfileApi.GetCurrentUsersProfile(GetAccessToken());
            var playlist = await _playlistsApi.CreatePlaylist(user.Id, new PlaylistDetails
            {
                Name = name,
                Public = false,
            }, GetAccessToken());

            for (int i = 0; i < trackUris.Count(); i+= 100)
            {
                var trackUris100 = trackUris.Skip(i).Take(100).ToArray();
                await _playlistsApi.AddItemsToPlaylist(playlist.Id, trackUris100, accessToken: GetAccessToken());
            }
        }

        private static IEnumerable<TrackRate> Order(IEnumerable<TrackRate> tracksRate, bool desc = true)
        {
            return desc ? tracksRate.OrderByDescending(e => e.Popularity) : tracksRate.OrderBy(e=>e.Popularity);
        }
        private string? GetAccessToken() => _memoryCache.Get(nameof(BearerAccessToken.AccessToken)).ToString();
    }
}
