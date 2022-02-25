using Microsoft.AspNetCore.Mvc;

namespace SpotifyGuess.Controllers
{
    public class SpotifyPlayerController : Controller
    {
        readonly ISpotifyPlayer _spotifyPlayer;

        public SpotifyPlayerController(ISpotifyPlayer spotifyPlayer) => _spotifyPlayer = spotifyPlayer;

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
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> PlayShuffle()
        {
            var tracks = await _spotifyPlayer.GetCurrentUsersTracks();
            var randomTrack = tracks.ElementAt(Random.Shared.Next(tracks.Count()));
            await _spotifyPlayer.PlayTracks(randomTrack.Track.Id);
            return RedirectToAction(nameof(Index));
        }
    }
}