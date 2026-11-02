using System;
using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.VersionDto
{
    /// <summary>Версия ПО (ответ GET /Version/GetAll, /Version/Search).</summary>
    public record VersionResponseDto
    {
        public int Id { get; init; }

        public int Program_id { get; init; }
        public string? ProgramName { get; init; }

        public DateTime Release_dateTime { get; init; }

        /// <summary>Признак "обязательная к установке" версия (end_point).</summary>
        public bool End_point { get; init; }

        /// <summary>SHA-256 хеш файла (64 hex-символа).</summary>
        [StringLength(64)]
        public string FileHash { get; init; } = null!;

        public long FileSize { get; init; }

        [Required]
        public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload PATCH /Version/UpdateIsEndPoint.</summary>
    public record UpdateIsEndPointDto
    {
        [Required][Range(1, int.MaxValue)] public int id { get; init; }
        [Required] public bool end_point { get; init; }
        [Required] public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload DELETE /Version/Delete.</summary>
    public record DeleteVersionDto
    {
        [Required][Range(1, int.MaxValue)] public int id { get; init; }
        [Required] public int ConcurrencyStamp { get; init; }
    }
}
