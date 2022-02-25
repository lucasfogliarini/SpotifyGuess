using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace SpotifyGuess.Controllers
{
    public class SpotifyPlayerController : Controller
    {
        readonly ISpotifyPlayer _spotifyPlayer;
        readonly IMemoryCache _memoryCache;

        public SpotifyPlayerController(ISpotifyPlayer spotifyPlayer, IMemoryCache memoryCache)
        {
            _spotifyPlayer = spotifyPlayer;
            _memoryCache = memoryCache;
        }

        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> Login(string? code, string? state)
        {
            var url = await _spotifyPlayer.Login(code);
            if (state == null)
            {
                return Redirect(url);
            }
            else
            {
                return View(nameof(Index), Enumerable.Empty<string>());
            }
        }
        public async Task<IActionResult> PlayShuffle()
        {
            try
            {
                var tracks = await _memoryCache.GetOrCreate("tracks", async (e) =>
                {
                    var tracks = await _spotifyPlayer.GetCurrentUsersTracks();
                    return tracks.Select(x => x.Track.Id);
                });

                var randomTrackId = tracks.ElementAt(Random.Shared.Next(tracks.Count()));
                await _spotifyPlayer.PlayTracks(randomTrackId);
                return View(nameof(Index), tracks);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(nameof(Index));
            }
        }
    }
}