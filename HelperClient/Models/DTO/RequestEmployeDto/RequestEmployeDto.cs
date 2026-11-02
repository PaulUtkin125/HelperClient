using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.RequestEmployeDto
{
    /// <summary>Payload POST /HelperAPI/RequestEmploye/PinToEmploye.</summary>
    public record PinToEmployeDto
    {
        [Required][Range(1, int.MaxValue)] public int employeId { get; init; }
        [Required][Range(1, int.MaxValue)] public int requestId { get; init; }
    }

    /// <summary>Payload POST /HelperAPI/RequestEmploye/UnpinToEmploye.</summary>
    public record UnpinToEmployeDto
    {
        [Required][Range(1, int.MaxValue)] public int employeId { get; init; }
        [Required][Range(1, int.MaxValue)] public int requestId { get; init; }
        [Required] public int concurrencyStamp { get; init; }
    }

    /// <summary>Payload POST /HelperAPI/RequestEmploye/UpdatePinToEmloye.</summary>
    public record UpdatePinToEmployeDto
    {
        [Required][Range(1, int.MaxValue)] public int oldEmployeId { get; init; }
        [Required][Range(1, int.MaxValue)] public int oldRequestId { get; init; }
        [Required][Range(1, int.MaxValue)] public int newEmployeId { get; init; }
        [Required][Range(1, int.MaxValue)] public int newRequestId { get; init; }
        [Required] public int concurrencyStamp { get; init; }
    }
}
