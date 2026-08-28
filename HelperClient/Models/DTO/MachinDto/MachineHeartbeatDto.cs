using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.MachinDto
{
    public record MachineHeartbeatDto
    {
        [Required]
        [StringLength(18)]
        public string MAC_address {  get; init; }

    }
}
