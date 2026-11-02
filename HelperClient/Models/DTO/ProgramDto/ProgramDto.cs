namespace HelperClient.Models.DTO.ProgramDto
{
    /// <summary>
    /// Справочник Program: /HelperAPI/Program/{GetAll,Add,Delete,Update,Search}.
    /// Контракт идентичен ReferenceItemDto (id, name, ConcurrencyStamp).
    /// </summary>
    public sealed record ProgramDto : Common.ReferenceItemDto;

    public sealed record ProgramAddDto : Common.ReferenceAddDto;
    public sealed record ProgramUpdateDto : Common.ReferenceUpdateDto;
    public sealed record ProgramDeleteDto : Common.ReferenceDeleteDto;
}
