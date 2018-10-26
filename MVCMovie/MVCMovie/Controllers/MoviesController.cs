using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using MVCMovie.Models;
using System.Net.Http;


namespace MVCMovie.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MVCMovieContext _context;

        public MoviesController(MVCMovieContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string movieGenre, string searchString)
        {
            IQueryable<string> genreQuery = from m in _context.Movie
                                            orderby m.Genre
                                            select m.Genre;

            var movies = from m in _context.Movie
                         select m;

            if (!String.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.Title.Contains(searchString));
            }

            if (!String.IsNullOrEmpty(movieGenre))
            {
                movies = movies.Where(x => x.Genre == movieGenre);
            }

            var movieGenreVM = new MovieGenreViewModel();
            movieGenreVM.Genres = new SelectList(await genreQuery.Distinct().ToListAsync());
            movieGenreVM.Movies = await movies.ToListAsync();

            return View(movieGenreVM);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.ID == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Title,ReleaseDate,Genre,Price,Rating,PosterURL")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }


        public async Task<IActionResult> OMDBCreate(string movieName)
        {
            ViewData["exists"] = "false";
            if (movieName == null || movieName == "")
            {
                ViewData["MovieObject"] = "";
                return View();
            }
            else
            {

                HttpClient client = new HttpClient();
                string url = "http://www.omdbapi.com/?apikey=" + "a15b6097" + "&t=" + movieName;
                var response = await client.GetAsync(url);
                var data = await response.Content.ReadAsStringAsync();

                var json = JsonConvert.DeserializeObject(data).ToString();
                dynamic omdbMovie = JObject.Parse(json);

                ViewData["movie"] = json;

                ViewData["omdbMovie"] = omdbMovie;

                Movie movie = new Movie();
                try
                {
                    movie.Title = omdbMovie["Title"];
                    movie.ReleaseDate = omdbMovie["Released"];
                    movie.Genre = omdbMovie["Genre"].ToString().Split(',')[0];
                    movie.PosterURL = omdbMovie["Poster"];

                    ViewData["Rating"] = omdbMovie["Rated"];
                }
                catch
                {

                }

                return View(movie);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OMDBCreate([Bind("ID,Title,ReleaseDate,Genre,Price,Rating,PosterURL")] Movie movie)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!MovieTitleExists(movie.Title))
                    {
                        _context.Add(movie);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ViewData["exists"] = "true";
                    }
                }
                catch
                {
                    throw;
                }
            }


            return View(movie);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Title,ReleaseDate,Genre,Price,Rating,PosterURL")] Movie movie)
        {
            if (id != movie.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.ID == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            _context.Movie.Remove(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.ID == id);
        }

        private bool MovieTitleExists(string title)
        {
            return _context.Movie.Any(e => e.Title == title);
        }


    }
}