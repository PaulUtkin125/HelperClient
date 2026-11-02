namespace HelperClient.Models.DTO.DepartmentDto
{
    /// <summary>
    /// Справочник Department: /HelperAPI/Department/{GetAll,Add,Delete,Update,Search}.
    /// Контракт идентичен ReferenceItemDto (id, name, ConcurrencyStamp).
    /// </summary>
    public sealed record DepartmentDto : Common.ReferenceItemDto;

    public sealed record DepartmentAddDto : Common.ReferenceAddDto;
    public sealed record DepartmentUpdateDto : Common.ReferenceUpdateDto;
    public sealed record DepartmentDeleteDto : Common.ReferenceDeleteDto;
}
