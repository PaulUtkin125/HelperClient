using System.Collections.Generic;
using System.Threading.Tasks;
using HelperClient.Models.DTO.HousingDepartmentDto;

namespace HelperClient.Services
{
    /// <summary>
    /// /HelperAPI/HousingDepartment — связи «корпус ↔ отделение».
    /// Валидация сервера: машина не может находиться в корпусе, где не работает её отделение.
    /// </summary>
    public class HousingDepartmentService
    {
        private const string Base = "/HousingDepartment";
        private readonly ApiClient _api;

        public HousingDepartmentService(ApiClient api) => _api = api;

        /// <summary>GET /HousingDepartment/GetAll — список сопоставлений (доступен всем).</summary>
        public Task<List<HousingDepartmentDto>> GetAllAsync() =>
            _api.GetListAsync<HousingDepartmentDto>(Base + "/GetAll");

        /// <summary>POST /HousingDepartment/Add — комбинация housing_id+department_id уникальна (409 при дубле).</summary>
        public Task AddAsync(int housingId, int departmentId)
        {
            Validation.EnsurePositiveId(housingId, "housing_id");
            Validation.EnsurePositiveId(departmentId, "department_id");
            return _api.PostAsync(Base + "/Add",
                new HousingDepartmentAddDto { housing_id = housingId, department_id = departmentId });
        }

        /// <summary>DELETE /HousingDepartment/Delete — все поля обязательны + ConcurrencyStamp.</summary>
        public Task DeleteAsync(int housingId, int departmentId, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(housingId, "housing_id");
            Validation.EnsurePositiveId(departmentId, "department_id");
            return _api.DeleteAsync(Base + "/Delete",
                new HousingDepartmentDeleteDto
                {
                    housing_id = housingId,
                    department_id = departmentId,
                    ConcurrencyStamp = concurrencyStamp
                });
        }

        /// <summary>PUT /HousingDepartment/Update — при смене ключей старые значения передаются как часть payload.</summary>
        public Task UpdateAsync(int housingId, int departmentId, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(housingId, "housing_id");
            Validation.EnsurePositiveId(departmentId, "department_id");
            return _api.PutAsync(Base + "/Update",
                new HousingDepartmentUpdateDto
                {
                    housing_id = housingId,
                    department_id = departmentId,
                    ConcurrencyStamp = concurrencyStamp
                });
        }

        /// <summary>GET /HousingDepartment/Search — idHousing/idDepartment опциональны.</summary>
        public Task<List<HousingDepartmentDto>> SearchAsync(int? idHousing = null, int? idDepartment = null)
        {
            var query = ApiClient.BuildQuery(("idHousing", idHousing), ("idDepartment", idDepartment));
            return _api.GetListAsync<HousingDepartmentDto>(Base + "/Search" + query);
        }
    }
}
