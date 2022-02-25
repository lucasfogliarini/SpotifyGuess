using Microsoft.AspNetCore.Mvc;

namespace SpotifyGuess.Controllers
{
    public class HomeController : Controller
    {
        readonly ISpotifyPlayer _spotifyPlayer;

        public HomeController(ISpotifyPlayer spotifyPlayer) => _spotifyPlayer = spotifyPlayer;

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}