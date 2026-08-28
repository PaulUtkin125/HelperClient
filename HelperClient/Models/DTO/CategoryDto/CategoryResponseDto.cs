using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.CategoryDto
{
    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }

        public bool HasChildren { get; set; }

        [Required]
        public int ConcurrencyStamp { get; init; }
    }
}
