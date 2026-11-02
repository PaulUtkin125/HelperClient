using System;
using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.EventDto
{
    /// <summary>Запись события из GET /HelperAPI/Event/GetAll.</summary>
    public record EventResponseDto
    {
        public int Id { get; init; }

        [StringLength(255)]
        public string Description { get; init; } = null!;

        public DateTime Create_DateTime { get; init; }
    }

    /// <summary>Payload для POST /HelperAPI/Event/Add.</summary>
    public record EventAddDto
    {
        [Required]
        [StringLength(255)]
        public string Description { get; init; } = null!;
    }

    /// <summary>Payload для DELETE /HelperAPI/Event/Delete.</summary>
    public record EventDeleteDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; init; }
    }
}
