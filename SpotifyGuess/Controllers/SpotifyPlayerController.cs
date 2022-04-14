﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SpotifyApi.NetCore.Authorization;

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

        public IActionResult Guess()
        {
            return View();
        }
        public async Task<IActionResult> Index(string? code, string? state)
        {
            var url = await _spotifyPlayer.Login(code);
            if (state == null)
            {
                return Redirect(url);
            }
            else
            {
                return View();
            }
        }

        public IActionResult LoadTracks(string? playlistName = null)
        {
            try
            {
                var tracks = _spotifyPlayer.GetCurrentUsersTracks(playlistName).Result;
                var tracksRate =  tracks.Select(e => new TrackRate
                {
                    Id = e.Track.Id,
                    Name = e.Track.Name,
                    Uri = e.Track.Uri,
                    Artists = string.Join(',', e.Track.Artists.Select(e => e.Name)),
                    Popularity = e.Track.Popularity,
                    Age = e.Track.Album?.ReleaseDate == null ? 0 : int.Parse(e.Track.Album.ReleaseDate[..4])
                }).DistinctBy(e=>e.Uri);

                _memoryCache.Set("tracks", tracksRate);
                return View(nameof(Index), tracksRate);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(nameof(Index));
            }
        }

        public async Task<IActionResult> PlayShuffle(string rate = "pop", bool desc = true)
        {
            try
            {
                var tracks = _memoryCache.Get<IEnumerable<TrackRate>>("tracks");
                IOrderedEnumerable<TrackRate> tracksOrdered = null;
                if (rate == "pop")
                {
                    tracksOrdered = tracks.OrderBy(e=>e.Popularity);
                } 
                else if (rate == "age")
                {
                    tracksOrdered = tracks.OrderBy(e => e.Age);
                }

                var randomTrack = tracksOrdered.ElementAt(Random.Shared.Next(tracksOrdered.Count()));
                await _spotifyPlayer.PlayTracks(randomTrack.Id);
                return View(nameof(Index), tracks);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(nameof(Index));
            }
        }

        public async Task<IActionResult> CreatePlaylist(string rate = "age")
        {
            try
            {
                var tracks = _memoryCache.Get<IEnumerable<TrackRate>>("tracks");
                IOrderedEnumerable<TrackRate> tracksOrdered = null;
                if (rate == "pop")
                {
                    tracksOrdered = tracks.OrderByDescending(e => e.Popularity);
                }
                else if (rate == "age")
                {
                    tracksOrdered = tracks.OrderBy(e => e.Age);
                }

                await _spotifyPlayer.CreatePlaylist("A origem", tracksOrdered.Select(e=>e.Uri).ToArray());

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