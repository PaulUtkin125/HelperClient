using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.Common
{
    /// <summary>
    /// Универсальный элемент справочников Application, Category, Department,
    /// Housing, Status, Employe (GET /GetAll возвращает id + name + ConcurrencyStamp).
    /// </summary>
    public record ReferenceItemDto
    {
        public int Id { get; init; }

        [Required]
        [StringLength(255)]
        public string Name { get; init; } = null!;

        /// <summary>Номер версии записи для оптимистичного контроля параллелизма.</summary>
        public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload для POST /Add — id строго 0.</summary>
    public record ReferenceAddDto
    {
        public int Id { get; init; }              // должен быть 0
        [Required][StringLength(255)] public string Name { get; init; } = null!;
    }

    /// <summary>Payload для PUT /Update.</summary>
    public record ReferenceUpdateDto
    {
        [Range(1, int.MaxValue)] public int Id { get; init; }
        [Required][StringLength(255)] public string Name { get; init; } = null!;
        [Required] public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload для DELETE /Delete.</summary>
    public record ReferenceDeleteDto
    {
        [Range(1, int.MaxValue)] public int Id { get; init; }
        [Required] public int ConcurrencyStamp { get; init; }
    }
}
