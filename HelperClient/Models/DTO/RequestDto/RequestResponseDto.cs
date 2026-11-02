using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.RequestDto
{
    /// <summary>Заявка на обслуживание (ответ GET /Request/GetAll, /Request/Search).</summary>
    public record RequestResponseDto
    {
        public int Id { get; init; }

        public int MachineId { get; init; }
        public string? MachineName { get; init; }

        public DateTime Create_DateTime { get; init; }

        public int CategoryId { get; init; }
        public string? CategoryName { get; init; }

        public int StatusId { get; init; }
        public string? StatusName { get; init; }

        [StringLength(255)]
        public string? Comment { get; init; }

        [StringLength(255)]
        public string? Phone_number { get; init; }

        public DateTime? End_DateTime { get; init; }

        /// <summary>Прикреплённые сотрудники (связь Request_Employe).</summary>
        public List<string> Employees { get; init; } = new();

        [Required]
        public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload PUT /Request/Update — полное обновление (перезапись), id обязателен.</summary>
    public record UpdateRequestDto
    {
        [Required][Range(1, int.MaxValue)] public int Id { get; init; }
        [Range(1, int.MaxValue)] public int MachineId { get; init; }
        [Required] public string Create_DateTime { get; init; } = null!;   // ISO 8601
        [Range(1, int.MaxValue)] public int CategoryId { get; init; }
        [Range(1, int.MaxValue)] public int StatusId { get; init; }
        [MaxLength(255)][MinLength(1)] public string? Comment { get; init; }
        [Phone] public string? Phone_number { get; init; }
        public string? End_DateTime { get; init; }                          // ISO 8601 или null
        [Required] public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload DELETE /Request/Delete.</summary>
    public record DeleteRequestDto
    {
        [Required][Range(1, int.MaxValue)] public int Id { get; init; }
        [Required] public int ConcurrencyStamp { get; init; }
    }
}
