using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pelican.Models;

public class Category
{
    [Key]
    public int Id { get; set; }
    [Required]
    [MaxLength(30)]
    [DisplayName("Category Name")]
    public string? Name { get; set; }
    [DisplayName("Display Order")]
    [Range(1, 100, ErrorMessage ="Display Order必須介於1~100之間")]
    public int DisplayOrder { get; set; }
}
