using SpotifyApi.NetCore;
using SpotifyApi.NetCore.Authorization;

namespace SpotifyGuess
{
    public static class AddSpotifyPlayerExtension
    {
        public static void AddSpotifyPlayer(this IServiceCollection services)
        {
            services.AddTransient<ISpotifyPlayer, SpotifyPlayer>();
            services.AddTransient<IPlaylistsApi, PlaylistsApi>();
            services.AddTransient<IPlayerApi, PlayerApi>();
            services.AddTransient<ITracksApi, TracksApi>();
            services.AddTransient<IUsersProfileApi, UsersProfileApi>();
            services.AddHttpClient();

            //how to configure: https://github.com/Ringobot/SpotifyApi.NetCore#user-authorization
            //app: https://developer.spotify.com/dashboard/applications/8d853260f0b7480889c1d6e43bc83676
            services.AddTransient<IUserAccountsService, UserAccountsService>();
        }
    }
}
