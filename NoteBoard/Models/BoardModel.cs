using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NoteBoard.Models
{
    public class BoardModel
    {
        public string? Id { get; init; }

        [Required]
        [MaxLength(100)]
        public string? Title { get; init; }

        [MaxLength(500)]
        public string? Description { get; init; }

        [BindNever]
        public DateTimeOffset CreationDate { get; init; }

        [BindNever]
        public DateTimeOffset LastEditDate { get; init; }

        [BindNever]
        public IEnumerable<NoteModel>? Notes { get; init; }
    }
}
