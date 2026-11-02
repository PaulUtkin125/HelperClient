namespace HelperClient.Models.DTO.HousingDto
{
    /// <summary>
    /// Справочник Housing: /HelperAPI/Housing/{GetAll,Add,Delete,Update,Search}.
    /// Контракт идентичен ReferenceItemDto (id, name, ConcurrencyStamp).
    /// </summary>
    public sealed record HousingDto : Common.ReferenceItemDto;

    public sealed record HousingAddDto : Common.ReferenceAddDto;
    public sealed record HousingUpdateDto : Common.ReferenceUpdateDto;
    public sealed record HousingDeleteDto : Common.ReferenceDeleteDto;
}
