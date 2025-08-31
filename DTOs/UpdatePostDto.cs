using System.ComponentModel.DataAnnotations;

namespace webapi.DTOs;

public class UpdatePostDto
{
    [Required]
    public int Id { get; set; }
    
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;
}
