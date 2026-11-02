using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.SimpleDto
{
    /// <summary>
    /// Универсальный ответ простых справочников (Department, Housing, Status...).
    /// </summary>
    public record IdNameResponseDto
    {
        public int Id { get; init; }

        [Required]
        [StringLength(255)]
        public string Name { get; init; } = null!;
    }
}
