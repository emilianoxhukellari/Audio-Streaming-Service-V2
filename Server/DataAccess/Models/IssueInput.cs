using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    public class IssueInput
    {
        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; }
        [Required]
        [Display(Name = "Issue Type")]
        public string Type { get; set; }
        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }
    }
}
