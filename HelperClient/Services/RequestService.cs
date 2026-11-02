using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HelperClient.Models.DTO.RequestDto;

namespace HelperClient.Services
{
    /// <summary>
    /// /HelperAPI/Request — заявки на обслуживание.
    /// Дублирование заявок допускается (в отличие от прочих таблиц).
    /// </summary>
    public class RequestService
    {
        private const string Base = "/Request";
        private readonly ApiClient _api;

        public RequestService(ApiClient api) => _api = api;

        /// <summary>GET /Request/GetAll — полный список заявок (только авторизованные).</summary>
        public Task<List<RequestResponseDto>> GetAllAsync() =>
            _api.GetListAsync<RequestResponseDto>(Base + "/GetAll");

        /// <summary>POST /Request/Add — создание заявки (доступно всем пользователям).</summary>
        public Task AddAsync(CreateRequestDto request)
        {
            Validation.EnsurePositiveId(request.MachineId, "machineId");
            Validation.EnsurePositiveId(request.CategoryId, "categoryId");
            Validation.EnsurePositiveId(request.StatusId, "statusId");

            var created = Validation.ParseIsoDate(request.Create_DateTime, "create_DateTime");
            Validation.EnsureNotFuture(created, "create_DateTime");

            Validation.EnsureOptionalText(request.Comment, "comment");
            Validation.EnsureOptionalText(request.Phone_number, "phone_number");

            if (request.End_DateTime is not null)
            {
                var end = Validation.ParseIsoDate(request.End_DateTime, "end_DateTime");
                Validation.EnsureNotFuture(end, "end_DateTime");
            }

            return _api.PostAsync(Base + "/Add", request);
        }

        /// <summary>DELETE /Request/Delete — id + ConcurrencyStamp (409 при изменённой записи).</summary>
        public Task DeleteAsync(int id, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(id, "id");
            return _api.DeleteAsync(Base + "/Delete",
                new DeleteRequestDto { Id = id, ConcurrencyStamp = concurrencyStamp });
        }

        /// <summary>PUT /Request/Update — полное обновление (перезапись) неизменяемые поля — старыми значениями.</summary>
        public Task UpdateAsync(UpdateRequestDto request)
        {
            Validation.EnsurePositiveId(request.Id, "id");
            var created = Validation.ParseIsoDate(request.Create_DateTime, "create_DateTime");
            Validation.EnsureNotFuture(created, "create_DateTime");
            Validation.EnsureOptionalText(request.Comment, "comment");
            Validation.EnsureOptionalText(request.Phone_number, "phone_number");
            if (request.End_DateTime is not null)
                Validation.EnsureNotFuture(Validation.ParseIsoDate(request.End_DateTime, "end_DateTime"), "end_DateTime");

            return _api.PutAsync(Base + "/Update", request);
        }

        /// <summary>GET /Request/Search — все критерии опциональны.</summary>
        public Task<List<RequestResponseDto>> SearchAsync(
            int? machineId = null,
            string? createDateTime = null,
            int? categoryId = null,
            int? statusId = null,
            string? endDateTime = null)
        {
            if (createDateTime is not null)
                Validation.EnsureNotFuture(Validation.ParseIsoDate(createDateTime, "Create_dateTime"), "Create_dateTime");
            if (endDateTime is not null)
                Validation.EnsureNotFuture(Validation.ParseIsoDate(endDateTime, "End_DateTime"), "End_DateTime");

            var query = ApiClient.BuildQuery(
                ("Machine_id", machineId),
                ("Create_dateTime", createDateTime),
                ("CategoryId", categoryId),
                ("StatusId", statusId),
                ("End_DateTime", endDateTime));

            return _api.GetListAsync<RequestResponseDto>(Base + "/Search" + query);
        }

        /// <summary>Удобная обёртка: создать заявку «сейчас» с текущим статусом.</summary>
        public Task AddNewAsync(int machineId, int categoryId, int statusId, string? comment = null, string? phoneNumber = null) =>
            AddAsync(new CreateRequestDto
            {
                MachineId = machineId,
                CategoryId = categoryId,
                StatusId = statusId,
                Create_DateTime = Validation.ToIsoUtc(DateTime.Now),
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                Phone_number = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
                End_DateTime = null
            });
    }
}
