using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NoteBoard.Data;

namespace NoteBoard.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;

        public HomeController(SignInManager<AppUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            if (_signInManager.IsSignedIn(User))
            {
                // User is signed in, redirect to the board list.
                return RedirectToAction("List", "Board");
            }

            // User is not signed in, redirect to the login page.
            return RedirectToAction("Login", "Account");
        }
    }
}
