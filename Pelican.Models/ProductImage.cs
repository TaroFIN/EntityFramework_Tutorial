using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pelican.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        [Required]
        public string ImageUrl { get; set; }
        [ForeignKey("ProductId")]
        public int ProductId { get; set; }
    }
}
