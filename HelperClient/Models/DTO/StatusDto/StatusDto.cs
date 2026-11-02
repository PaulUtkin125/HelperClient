namespace HelperClient.Models.DTO.StatusDto
{
    /// <summary>
    /// Справочник Status: /HelperAPI/Status/{GetAll,Add,Delete,Update,Search}.
    /// Контракт идентичен ReferenceItemDto (id, name, ConcurrencyStamp).
    /// </summary>
    public sealed record StatusDto : Common.ReferenceItemDto;

    public sealed record StatusAddDto : Common.ReferenceAddDto;
    public sealed record StatusUpdateDto : Common.ReferenceUpdateDto;
    public sealed record StatusDeleteDto : Common.ReferenceDeleteDto;
}
