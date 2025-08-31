using System.ComponentModel.DataAnnotations;

namespace webapi.DTOs;

public class UpdatePostDto
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Body { get; set; } = string.Empty;
}
