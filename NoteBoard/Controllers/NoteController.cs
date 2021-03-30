using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteBoard.Data;
using NoteBoard.Models;

namespace NoteBoard.Controllers
{
    public class NoteController : Controller
    {
        private readonly AppDbContext _dbContext;

        public NoteController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
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
                if (!TryGetAccessToken(out string? accessToken))
                {
                    SetAccessToken();

                    return Json(Enumerable.Empty<int>());
                }

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
                if (!TryGetAccessToken(out string? accessToken))
                {
                    return Json(new SuccessResponse
                    {
                        Message = "The cookie for the access token is not set, do you have cookies disabled?"
                    });
                }

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

                return Json(new SuccessResponse<int> { Success = true, Value = note.Id });
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
                if (!TryGetAccessToken(out string? accessToken))
                {
                    return Json(new SuccessResponse
                    {
                        Message = "The cookie for the access token is not set, do you have cookies disabled?"
                    });
                }

                if (accessToken != note.AccessToken)
                {
                    return Json(new SuccessResponse { Message = "You do not have access to this note." });
                }

                note.Caption = model.Caption?.Trim() ?? string.Empty;
                note.Content = model.Content?.Trim() ?? string.Empty;
                note.LastEditDate = DateTimeOffset.Now;

                await _dbContext.SaveChangesAsync();

                return Json(new SuccessResponse { Success = true });
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

        private static bool IsAccessTokenValid([NotNullWhen(true)] string? accessToken)
        {
            if (accessToken == null)
            {
                return false;
            }

            // 4 * ceiling(n / 3)
            const int base64Length = 44;

            if (accessToken.Length > base64Length)
            {
                return false;
            }

            Span<byte> dummy = stackalloc byte[32];

            return Convert.TryFromBase64String(accessToken, dummy, out _);
        }

        private void SetAccessToken()
        {
            var options = new CookieOptions
            {
                // Dirty solution to make the cookie last for a very long time, if possible.
                Expires = DateTimeOffset.FromUnixTimeSeconds(int.MaxValue),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            string accessToken = GenerateAccessToken();
            Response.Cookies.Append("NoteAccessToken", accessToken, options);
        }

        private bool TryGetAccessToken([NotNullWhen(true)] out string? accessToken)
        {
            accessToken = Request.Cookies["NoteAccessToken"];

            return IsAccessTokenValid(accessToken);
        }

        private class SuccessResponse
        {
            public bool Success { get; init; }

            public string Message { get; init; }
        }

        private class SuccessResponse<T> : SuccessResponse
        {
            public T Value { get; init; }
        }
    }
}
