using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NoteBoard.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
    }
}
