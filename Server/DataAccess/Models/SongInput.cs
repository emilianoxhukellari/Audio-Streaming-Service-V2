using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class SongInput
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Song Name")]
        public string SongName { get; set; }
        [Required]
        [StringLength(200)]
        [Display(Name = "Artist Name")]
        public string ArtistName { get; set; }
        [Required]
        [Display(Name = "Image File")]
        [AllowedExtensions(new string[] { ".png" })]
        public IFormFile ImageFile { get; set; }
        [Required]
        [Display(Name = "Song File")]
        [AllowedExtensions(new string[] { ".wav" })]
        public IFormFile SongFile { get; set; }
    }

    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName);
                if (!_extensions.Contains(extension.ToLower()))
                {
                    return new ValidationResult($"Only the following extensions are allowed: {string.Join(", ", _extensions)}");
                }
            }

            return ValidationResult.Success;
        }
    }
}
