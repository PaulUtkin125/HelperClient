using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.MachinDto
{
    public record MachineResponseDto
    {
        public int Id { get; init; }
        
        public string MAC_address { get; init; } = null!;
        public List<string> IP_address { get; set; } = new();

        [Required]
        [StringLength(255)]
        public string Department_Name { get; init; } = null!;
        
        [Required]
        [Range(1, int.MaxValue)]
        public int MacineType { get; init; }

        [Required]
        [StringLength(255)]
        public string Housing { get; init; } = null!;

        [StringLength(255)]
        public string? Office_number { get; init; }

        [Required]
        public int ConcurrencyStamp { get; init; }
    }
}
