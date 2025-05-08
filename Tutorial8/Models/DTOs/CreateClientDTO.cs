using System.ComponentModel.DataAnnotations;

namespace Tutorial8.Models.DTOs;

public class CreateClientDTO
{
    [Required]
    [MaxLength(120)]
    public string FirstName { get; set; }
    
    [Required]
    [MaxLength(120)]
    public string LastName { get; set; }
    
    [Required]
    [MaxLength(120)]
    public string Email { get; set; }
    
    [Required]
    [MaxLength(120)]
    public string Telephone { get; set; }
    
    [Required]
    [MaxLength(120)]
    public string Pesel { get; set; }
}