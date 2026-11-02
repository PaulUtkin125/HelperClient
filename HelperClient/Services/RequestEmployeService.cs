using System.Threading.Tasks;
using HelperClient.Models.DTO.RequestEmployeDto;

namespace HelperClient.Services
{
    /// <summary>
    /// /HelperAPI/RequestEmploye — прикрепление сотрудников к заявкам (связь многие-ко-многим).
    /// </summary>
    public class RequestEmployeService
    {
        private const string Base = "/RequestEmploye";
        private readonly ApiClient _api;

        public RequestEmployeService(ApiClient api) => _api = api;

        /// <summary>POST /PinToEmploye — FK должны существовать (404), дубль связи → 409.</summary>
        public Task PinAsync(int employeId, int requestId)
        {
            Validation.EnsurePositiveId(employeId, "employeId");
            Validation.EnsurePositiveId(requestId, "requestId");
            return _api.PostAsync(Base + "/PinToEmploye",
                new PinToEmployeDto { employeId = employeId, requestId = requestId });
        }

        /// <summary>POST /UnpinToEmploye — требуется актуальный concurrencyStamp (409 при конфликте).</summary>
        public Task UnpinAsync(int employeId, int requestId, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(employeId, "employeId");
            Validation.EnsurePositiveId(requestId, "requestId");
            return _api.PostAsync(Base + "/UnpinToEmploye",
                new UnpinToEmployeDto
                {
                    employeId = employeId,
                    requestId = requestId,
                    concurrencyStamp = concurrencyStamp
                });
        }

        /// <summary>POST /UpdatePinToEmloye — переназначение связи сотрудник↔заявка.</summary>
        public Task UpdatePinAsync(int oldEmployeId, int oldRequestId, int newEmployeId, int newRequestId, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(oldEmployeId, "oldEmployeId");
            Validation.EnsurePositiveId(oldRequestId, "oldRequestId");
            Validation.EnsurePositiveId(newEmployeId, "newEmployeId");
            Validation.EnsurePositiveId(newRequestId, "newRequestId");
            return _api.PostAsync(Base + "/UpdatePinToEmloye",
                new UpdatePinToEmployeDto
                {
                    oldEmployeId = oldEmployeId,
                    oldRequestId = oldRequestId,
                    newEmployeId = newEmployeId,
                    newRequestId = newRequestId,
                    concurrencyStamp = concurrencyStamp
                });
        }
    }
}
