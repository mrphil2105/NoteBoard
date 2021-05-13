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
            // Attempt to find a board with the specified id and include the notes in the query results.
            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);

            if (board != null)
            {
                // Construct a collection of NoteModel from the note entities.
                var noteModels = board.Notes.Select(n =>
                    new NoteModel { Id = n.Id, Caption = n.Caption, Content = n.Content });

                return Json(noteModels);
            }

            return NotFound();
        }

        public async Task<IActionResult> GetOwned(string boardId)
        {
            // Attempt to find a board with the specified id and include the notes in the query results.
            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);

            if (board != null)
            {
                // Check if the access token is set and get it if it is.
                if (!TryGetAccessToken(out string? accessToken))
                {
                    // The access token is not set, attempt to set it.
                    SetAccessToken();

                    // The user owns no notes so we return an empty collection.
                    return Json(Enumerable.Empty<int>());
                }

                // Return the ids of the notes that the user owns on the board.
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
                // We allow one of either caption or content to be white-space, but not both.
                string.IsNullOrWhiteSpace(model.Caption) && string.IsNullOrWhiteSpace(model.Content))
            {
                return BadRequest();
            }

            // Attempt to find a board with the specified id and include the notes in the query results.
            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);

            if (board != null)
            {
                const int maxNoteCount = 100;

                if (board.Notes.Count >= maxNoteCount)
                {
                    // Return a response indicating error with a message describing the issue.
                    return Json(new SuccessResponse
                    {
                        Message = $"This board has reached the maximum limit of {maxNoteCount} notes."
                    });
                }

                // Check if the access token is set and get it if it is.
                if (!TryGetAccessToken(out string? accessToken))
                {
                    // Return a response indicating error with a message describing the issue.
                    return Json(new SuccessResponse
                    {
                        Message = "The cookie for the access token is not set, do you have cookies disabled?"
                    });
                }

                var currentTime = DateTimeOffset.Now;

                // Construct a new Note with the model caption and content and other metadata.
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

                // Return a response indicating success and containing the assigned id.
                return Json(new SuccessResponse<int> { Success = true, Value = note.Id });
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromHeader] string boardId, [FromBody] NoteModel model)
        {
            if (!ModelState.IsValid ||
                // We allow one of either caption or content to be white-space, but not both.
                string.IsNullOrWhiteSpace(model.Caption) && string.IsNullOrWhiteSpace(model.Content))
            {
                return BadRequest();
            }

            // Attempt to find a board with the specified id and include the notes in the query results.
            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);
            // Attempt to find a note with the id in the model.
            var note = board?.Notes.SingleOrDefault(n => n.Id == model.Id);

            if (board != null && note != null)
            {
                if (!TryGetAccessToken(out string? accessToken))
                {
                    // Return a response indicating error with a message describing the issue.
                    return Json(new SuccessResponse
                    {
                        Message = "The cookie for the access token is not set, do you have cookies disabled?"
                    });
                }

                if (accessToken != note.AccessToken)
                {
                    // Return a response indicating error with a message describing the issue.
                    return Json(new SuccessResponse { Message = "You do not have access to this note." });
                }

                // Update caption and content from the model and set the edit date.
                note.Caption = model.Caption?.Trim() ?? string.Empty;
                note.Content = model.Content?.Trim() ?? string.Empty;
                note.LastEditDate = DateTimeOffset.Now;

                await _dbContext.SaveChangesAsync();

                // Return a response indicating success.
                return Json(new SuccessResponse { Success = true });
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromHeader] string boardId, [FromBody] int id)
        {
            // Attempt to find a board with the specified id and include the notes in the query results.
            var board = await _dbContext.Boards.Include(b => b.Notes)
                .SingleOrDefaultAsync(b => b.Id == boardId);
            var note = board?.Notes.SingleOrDefault(n => n.Id == id);

            if (board != null && note != null)
            {
                if (!TryGetAccessToken(out string? accessToken))
                {
                    // Return a response indicating error with a message describing the issue.
                    return Json(new SuccessResponse
                    {
                        Message = "The cookie for the access token is not set, do you have cookies disabled?"
                    });
                }

                if (accessToken != note.AccessToken)
                {
                    // Return a response indicating error with a message describing the issue.
                    return Json(new SuccessResponse { Message = "You do not have access to this note." });
                }

                _dbContext.Notes.Remove(note);
                await _dbContext.SaveChangesAsync();

                // Return a response indicating success.
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

            // If the token is too long it is invalid.
            if (accessToken.Length > base64Length)
            {
                return false;
            }

            Span<byte> dummy = stackalloc byte[32];

            // Verify that the token is valid base64.
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
