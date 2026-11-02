using System.Collections.Generic;
using System.Threading.Tasks;
using HelperClient.Models.DTO.MachinDto;

namespace HelperClient.Services
{
    /// <summary>
    /// /HelperAPI/Machine — учёт оборудования.
    /// GetAll/Add/Delete/Update/Search доступны только авторизованным,
    /// Sync/Heartbeat — всем пользователям (используются самим клиентом на устройстве).
    /// </summary>
    public class MachineService
    {
        private const string Base = "/Machine";
        private readonly ApiClient _api;

        public MachineService(ApiClient api) => _api = api;

        /// <summary>GET /Machine/GetAll — список зарегистрированных устройств.</summary>
        public Task<List<MachineResponseDto>> GetAllAsync() =>
            _api.GetListAsync<MachineResponseDto>(Base + "/GetAll");

        /// <summary>POST /Machine/Add — регистрация устройства; уникальность по MAC_address (409 при дубле).</summary>
        public Task AddAsync(CreateMachineDto machine)
        {
            ValidateCore(machine.MAC_address, machine.IP_address, machine.Department_id, machine.Housing_id);
            Validation.EnsureOptionalText(machine.Office_number, "office_number");
            return _api.PostAsync(Base + "/Add", machine);
        }

        /// <summary>DELETE /Machine/Delete — удаление по внутреннему id + ConcurrencyStamp.</summary>
        public Task DeleteAsync(int id, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(id, "id");
            return _api.DeleteAsync(Base + "/Delete", new { id, ConcurrencyStamp = concurrencyStamp });
        }

        /// <summary>PUT /Machine/Update — полное обновление; неизменные поля передавать старыми значениями.</summary>
        public Task UpdateAsync(MachineSyncDto machine)
        {
            ValidateCore(machine.MAC_address, machine.IP_address, machine.Department_id, machine.Housing_id);
            Validation.EnsureOptionalText(machine.Office_number, "office_number");
            return _api.PutAsync(Base + "/Update", machine);
        }

        /// <summary>GET /Machine/Search?IP=…&amp;MAC=… — оба параметра опциональны.</summary>
        public Task<List<MachineResponseDto>> SearchAsync(string? ip = null, string? mac = null)
        {
            if (ip is not null) Validation.EnsureValidIp(ip, "IP");
            if (mac is not null) Validation.EnsureValidMac(mac, "MAC");
            var query = ApiClient.BuildQuery(("IP", ip), ("MAC", mac));
            return _api.GetListAsync<MachineResponseDto>(Base + "/Search" + query);
        }

        /// <summary>
        /// POST /Machine/Sync — автоматическая актуализация: нет машины с таким MAC → создание,
        /// иначе — обновление данных. Требует актуальный ConcurrencyStamp для обновления.
        /// </summary>
        public Task SyncAsync(MachineSyncDto machine)
        {
            ValidateCore(machine.MAC_address, machine.IP_address, machine.Department_id, machine.Housing_id);
            Validation.EnsureOptionalText(machine.Office_number, "office_number");
            return _api.PostAsync(Base + "/Sync", machine);
        }

        /// <summary>PATCH /Machine/Heartbeat — отметка времени последнего отклика по MAC.</summary>
        public Task HeartbeatAsync(string macAddress)
        {
            Validation.EnsureValidMac(macAddress, "MAC_address");
            return _api.PatchAsync(Base + "/Heartbeat", new MachineHeartbeatDto { MAC_address = macAddress.Trim() });
        }

        private static void ValidateCore(string mac, List<string> ips, int departmentId, int housingId)
        {
            Validation.EnsureValidMac(mac, "MAC_address");
            if (ips is null || ips.Count == 0)
                throw new System.ArgumentException("IP_address: требуется хотя бы один IP-адрес.");
            foreach (var ip in ips)
                Validation.EnsureValidIp(ip, "IP_address");
            Validation.EnsurePositiveId(departmentId, "department_id");
            Validation.EnsurePositiveId(housingId, "housing_id");
        }
    }
}
