using Microsoft.AspNetCore.Mvc;

namespace NoteBoard.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
