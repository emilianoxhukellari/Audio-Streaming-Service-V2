using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

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
        public string SubmitterId { get; set; }
        [AllowNull]
        public string? ResolverId { get; set; }
        [AllowNull]
        public string? SolutionDescription { get; set; }
        public StreamingUser Submitter { get; set; }
        public StreamingUser Resolver { get; set; }
    }
}
