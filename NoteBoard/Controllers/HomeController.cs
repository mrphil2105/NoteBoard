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
                return RedirectToAction("List", "Board");
            }

            return RedirectToAction("Login", "Account");
        }
    }
}
