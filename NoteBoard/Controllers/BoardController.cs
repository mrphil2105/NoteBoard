using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NoteBoard.Data;
using NoteBoard.Models;

namespace NoteBoard.Controllers
{
    public class BoardController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public BoardController(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string id)
        {
            var board = await _dbContext.Boards.FindAsync(id);

            if (board != null)
            {
                var boardModel = new BoardModel
                {
                    Id = board.Id,
                    Title = board.Title,
                    Description = board.Description,
                    LastEditDate = board.LastEditDate
                };

                return View(boardModel);
            }

            return View();
        }

        //
        // Board management
        //

        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(BoardModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                string boardId = GenerateBoardId();
                var currentTime = DateTimeOffset.Now;

                var board = new Board
                {
                    Id = boardId,
                    Title = model.Title?.Trim() ?? string.Empty,
                    Description = model.Description?.Trim() ?? string.Empty,
                    CreationDate = currentTime,
                    LastEditDate = currentTime,
                    User = user
                };

                // ReSharper disable once MethodHasAsyncOverload
                _dbContext.Boards.Add(board);
                await _dbContext.SaveChangesAsync();

                return RedirectToRoute("board", new { id = boardId });
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> List()
        {
            string username = User.FindFirstValue(ClaimTypes.Name);
            var user = await _dbContext.Users.Include(u => u.Boards)
                .SingleOrDefaultAsync(u => u.UserName == username);

            if (user.Boards.Count > 0)
            {
                var boardModels = user.Boards.Select(b => new BoardModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    CreationDate = b.CreationDate,
                    LastEditDate = b.LastEditDate
                });

                return View(boardModels);
            }

            return View();
        }

        private static string GenerateBoardId()
        {
            Span<byte> bytes = stackalloc byte[12];
            RandomNumberGenerator.Fill(bytes);

            return WebEncoders.Base64UrlEncode(bytes);
        }
    }
}
