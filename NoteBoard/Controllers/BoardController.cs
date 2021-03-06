﻿using System;
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
            // Attempt to find the board with the specified id.
            var board = await _dbContext.Boards.FindAsync(id);

            if (board != null)
            {
                // Construct a BoardModel from the board entity.
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
                // Get the currently logged in user.
                var user = await _userManager.GetUserAsync(User);
                string boardId = GenerateBoardId();
                var currentTime = DateTimeOffset.Now;

                // Construct a new Board with the generated id and model title, model description and metadata.
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

                // Redirect to the board viewing page.
                return RedirectToRoute("board", new { id = boardId });
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> List()
        {
            // Find the currently logged in user's username via the claim.
            string username = User.FindFirstValue(ClaimTypes.Name);
            // Find the user entity using the username and include the boards in the query results.
            var user = await _dbContext.Users.Include(u => u.Boards)
                .SingleOrDefaultAsync(u => u.UserName == username);

            if (user.Boards.Count > 0)
            {
                // Construct a collection of BoardModel from the board entities.
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
