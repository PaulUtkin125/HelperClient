using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.ReportDto
{
    /// <summary>Диапазон свободных IP-адресов (ответ /Report/FoundFreeIP).</summary>
    public record FreeIpRangeDto
    {
        [Required] public string StartIp { get; init; } = null!;
        [Required] public string EndIp { get; init; } = null!;
        /// <summary>Количество адресов в диапазоне.</summary>
        public int Count { get; init; }
    }

    /// <summary>Машина с дублирующимся IP-адресом (ответ /Report/FoundDuplicateIP).</summary>
    public record DuplicateIpDto
    {
        [Required] public string IpAddress { get; init; } = null!;
        public List<DuplicateMachineDto> Machines { get; init; } = new();
    }

    public record DuplicateMachineDto
    {
        public int Id { get; init; }
        public string MAC_address { get; init; } = null!;
        public string? Office_number { get; init; }
    }
}
