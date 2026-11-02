using System.Collections.Generic;
using System.Threading.Tasks;
using HelperClient.Models.DTO.Common;

namespace HelperClient.Services
{
    /// <summary>
    /// Клиент группы 1 «Базовые справочники»: Application, Category, Department,
    /// Housing, Status, Employe. Все ресурсы имеют идентичный контракт CRUD.
    /// </summary>
    public class ReferenceService
    {
        private readonly ApiClient _api;

        public ReferenceService(ApiClient api) => _api = api;

        /// <param name="resource">Имя ресурса в пути: Application | Category | Department | Housing | Status | Employe.</param>
        private string Path(string resource) => $"/{resource}";

        /// <summary>GET /HelperAPI/{Resource}/GetAll — полный справочник (доступен всем).</summary>
        public Task<List<ReferenceItemDto>> GetAllAsync(string resource) =>
            _api.GetListAsync<ReferenceItemDto>(Path(resource) + "/GetAll");

        /// <summary>
        /// POST /HelperAPI/{Resource}/Add — id строго 0, name 1–255 символов.
        /// 409 Conflict при дубликате name.
        /// </summary>
        public Task AddAsync(string resource, string name)
        {
            Validation.EnsureNewEntityId(0);
            Validation.EnsureRequiredText(name, "name");
            return _api.PostAsync(Path(resource) + "/Add", new ReferenceAddDto { Id = 0, Name = name.Trim() });
        }

        /// <summary>DELETE /HelperAPI/{Resource}/Delete — id &gt; 0 + актуальный ConcurrencyStamp.</summary>
        public Task DeleteAsync(string resource, int id, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(id, "id");
            return _api.DeleteAsync(Path(resource) + "/Delete",
                new ReferenceDeleteDto { Id = id, ConcurrencyStamp = concurrencyStamp });
        }

        /// <summary>PUT /HelperAPI/{Resource}/Update — переименование существующего элемента.</summary>
        public Task UpdateAsync(string resource, int id, string name, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(id, "id");
            Validation.EnsureRequiredText(name, "name");
            return _api.PutAsync(Path(resource) + "/Update",
                new ReferenceUpdateDto { Id = id, Name = name.Trim(), ConcurrencyStamp = concurrencyStamp });
        }

        /// <summary>GET /HelperAPI/{Resource}/Search?searchString=… — поиск по вхождению подстроки в name.</summary>
        public Task<List<ReferenceItemDto>> SearchAsync(string resource, string searchString)
        {
            Validation.EnsureRequiredText(searchString, "searchString");
            var query = ApiClient.BuildQuery(("searchString", searchString.Trim()));
            return _api.GetListAsync<ReferenceItemDto>(Path(resource) + "/Search" + query);
        }
    }
}
