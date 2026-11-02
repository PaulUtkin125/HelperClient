using System.Collections.Generic;
using System.Threading.Tasks;
using HelperClient.Models.DTO.EventDto;

namespace HelperClient.Services
{
    /// <summary>
    /// /HelperAPI/Event — учёт пользовательских событий (только авторизованные).
    /// </summary>
    public class EventService
    {
        private const string Base = "/Event";
        private readonly ApiClient _api;

        public EventService(ApiClient api) => _api = api;

        /// <summary>GET /Event/GetAll — список всех событий (401 без токена).</summary>
        public Task<List<EventResponseDto>> GetAllAsync() =>
            _api.GetListAsync<EventResponseDto>(Base + "/GetAll");

        /// <summary>POST /Event/Add — описание ≤255 символов, не пусто/пробелы.</summary>
        public Task AddAsync(string description)
        {
            Validation.EnsureRequiredText(description, "Description");
            return _api.PostAsync(Base + "/Add", new EventAddDto { Description = description.Trim() });
        }

        /// <summary>DELETE /Event/Delete — 404, если запись отсутствует.</summary>
        public Task DeleteAsync(int id)
        {
            Validation.EnsurePositiveId(id, "id");
            return _api.DeleteAsync(Base + "/Delete", new EventDeleteDto { Id = id });
        }
    }
}
