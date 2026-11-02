using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.HousingDepartmentDto
{
    /// <summary>Связь «корпус ↔ отделение» (таблица Housing_Department, многие-ко-многим).</summary>
    public record HousingDepartmentDto
    {
        [Range(1, int.MaxValue)]
        public int Housing_id { get; init; }

        [Range(1, int.MaxValue)]
        public int Department_id { get; init; }

        [Required]
        public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload POST /HelperAPI/HousingDepartment/Add.</summary>
    public record HousingDepartmentAddDto
    {
        [Required][Range(1, int.MaxValue)] public int housing_id { get; init; }
        [Required][Range(1, int.MaxValue)] public int department_id { get; init; }
    }

    /// <summary>Payload PUT /HelperAPI/HousingDepartment/Update (все поля обязательны).</summary>
    public record HousingDepartmentUpdateDto
    {
        [Required][Range(1, int.MaxValue)] public int housing_id { get; init; }
        [Required][Range(1, int.MaxValue)] public int department_id { get; init; }
        [Required] public int ConcurrencyStamp { get; init; }
    }

    /// <summary>Payload DELETE /HelperAPI/HousingDepartment/Delete.</summary>
    public record HousingDepartmentDeleteDto
    {
        [Required][Range(1, int.MaxValue)] public int housing_id { get; init; }
        [Required][Range(1, int.MaxValue)] public int department_id { get; init; }
        [Required] public int ConcurrencyStamp { get; init; }
    }
}
