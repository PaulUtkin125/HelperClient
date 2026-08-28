using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.MachinDto
{
    public record CreateMachineDto
    {
        [Required]
        public List<string> IP_address { get; init; } = new();
        
        [Required]
        [StringLength(18)]
        public string MAC_address { get; init; } = null!;

        [Required]
        [Range(1, int.MaxValue)]
        public int Department_id { get; init; }
       
        [Required]
        [Range(1, int.MaxValue)]
        public int MacineType { get; init; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Housing_id { get; init; }

        [MaxLength(255)]
        public string? Office_number { get; init; }
    }
}
