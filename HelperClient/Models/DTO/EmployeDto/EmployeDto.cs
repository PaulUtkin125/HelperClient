namespace HelperClient.Models.DTO.EmployeDto
{
    /// <summary>
    /// Справочник Employe: /HelperAPI/Employe/{GetAll,Add,Delete,Update,Search}.
    /// Контракт идентичен ReferenceItemDto (id, name, ConcurrencyStamp).
    /// </summary>
    public sealed record EmployeDto : Common.ReferenceItemDto;

    public sealed record EmployeAddDto : Common.ReferenceAddDto;
    public sealed record EmployeUpdateDto : Common.ReferenceUpdateDto;
    public sealed record EmployeDeleteDto : Common.ReferenceDeleteDto;
}
