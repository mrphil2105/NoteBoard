using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteBoard.Data;
using NoteBoard.Models;

namespace NoteBoard.Controllers
{
    public class NoteController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public NoteController(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> GetAll(string boardId)
        {
            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);

            if (board != null)
            {
                var noteModels = board.Notes.Select(n =>
                    new NoteModel { Id = n.Id, Caption = n.Caption, Content = n.Content });

                return Json(noteModels);
            }

            return NotFound();
        }

        public async Task<IActionResult> GetOwned(string boardId)
        {
            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);

            if (board != null)
            {
                string accessToken = GetAccessToken();
                var ids = board.Notes.Where(n => n.AccessToken == accessToken)
                    .Select(n => n.Id);

                return Json(ids);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromHeader] string boardId, [FromBody] NoteModel model)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(model.Caption) && string.IsNullOrWhiteSpace(model.Content))
            {
                return BadRequest();
            }

            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);

            if (board != null)
            {
                string accessToken = GetAccessToken();
                var currentTime = DateTimeOffset.Now;

                var note = new Note
                {
                    Caption = model.Caption?.Trim() ?? string.Empty,
                    Content = model.Content?.Trim() ?? string.Empty,
                    AccessToken = accessToken,
                    CreationDate = currentTime,
                    LastEditDate = currentTime,
                    Board = board
                };

                // ReSharper disable once MethodHasAsyncOverload
                _dbContext.Notes.Add(note);
                await _dbContext.SaveChangesAsync();

                return Json(note.Id);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromHeader] string boardId, [FromBody] NoteModel model)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(model.Caption) && string.IsNullOrWhiteSpace(model.Content))
            {
                return BadRequest();
            }

            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);
            var note = board?.Notes.SingleOrDefault(n => n.Id == model.Id);

            if (board != null && note != null)
            {
                string accessToken = GetAccessToken();

                if (accessToken != note.AccessToken)
                {
                    return Json(new { Success = false, Message = "You do not have access to this note." });
                }

                note.Caption = model.Caption?.Trim() ?? string.Empty;
                note.Content = model.Content?.Trim() ?? string.Empty;
                note.LastEditDate = DateTimeOffset.Now;

                await _dbContext.SaveChangesAsync();

                return Json(new { Success = true });
            }

            return NotFound();
        }

        //
        // Access token
        //

        private static string GenerateAccessToken()
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);

            return Convert.ToBase64String(bytes);
        }

        private static void CheckAccessToken(string? accessToken)
        {
            if (accessToken is null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            // 4 * ceiling(n / 3)
            const int base64Length = 44;

            if (accessToken.Length > base64Length)
            {
                throw new ArgumentException(
                    $"The specified access token cannot be greater than {base64Length} characters.",
                    nameof(accessToken));
            }

            Span<byte> dummy = stackalloc byte[32];

            if (!Convert.TryFromBase64String(accessToken, dummy, out _))
            {
                throw new ArgumentException("The specified access token is invalid.", nameof(accessToken));
            }
        }

        private bool EnsureAccessToken()
        {
            string? accessToken = Request.Cookies["NoteAccessToken"];

            if (string.IsNullOrEmpty(accessToken))
            {
                var options = new CookieOptions
                {
                    // Dirty solution to make the cookie last for a very long time, if possible.
                    Expires = DateTimeOffset.FromUnixTimeSeconds(int.MaxValue),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                };

                accessToken = GenerateAccessToken();
                Response.Cookies.Append("NoteAccessToken", accessToken, options);

                return true;
            }

            CheckAccessToken(accessToken);

            return false;
        }

        private string GetAccessToken()
        {
            string? accessToken = Request.Cookies["NoteAccessToken"];
            CheckAccessToken(accessToken);

            return accessToken!;
        }
    }
}
