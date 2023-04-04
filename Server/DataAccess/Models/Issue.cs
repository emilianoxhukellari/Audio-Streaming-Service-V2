using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class Issue
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string TitleNormalized { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public bool IsSolved { get; set; }
        [Required]
        public int SubmitterId { get; set; }
        [AllowNull]
        public int? ResolverId { get; set; }
        [AllowNull]
        public string? SolutionDescription { get; set; }
    }
}
