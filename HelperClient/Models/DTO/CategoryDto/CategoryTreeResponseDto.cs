using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.CategoryDto
{
    public class CategoryTreeResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }

        [Required]
        public int ConcurrencyStamp { get; init; }

        public List<CategoryTreeResponseDto> Children { get; set; } = new();
    }
}
