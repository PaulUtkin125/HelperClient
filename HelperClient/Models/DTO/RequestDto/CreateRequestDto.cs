using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.RequestDto
{
    public record CreateRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int MachineId { get; init; }

        [Required]
        public string Create_DateTime {get; init; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CategoryId { get; init; }

        [Required]
        [Range(1, int.MaxValue)]
        public int StatusId { get; init; }

        [MaxLength(255)]
        [MinLength(1)]
        public string? Comment { get; init; }

        [Phone]
        public string? Phone_number { get; init; }
        public string? End_DateTime { get; init; }
    }
}
