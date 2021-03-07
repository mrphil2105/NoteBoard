using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace NoteBoard.Data
{
    public class AppUser : IdentityUser
    {
        public ICollection<Board> Boards { get; set; } = null!;
    }
}
