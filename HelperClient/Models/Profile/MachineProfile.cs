using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using HelperClient.Services.Network;

namespace HelperClient.Models.Profile
{
    /// <summary>
    /// Профиль машины-клиента. Хранится в локальном файле конфигурации
    /// (%LocalAppData%/HelperClient или ~/.config/HelperClient) и заполняется при первом запуске.
    /// Поля соответствуют контракту /HelperAPI/Machine/Sync.
    /// </summary>
    public class MachineProfile
    {
        /// <summary>ID отделения (Department_id).</summary>
        [JsonPropertyName("departmentId")]
        public int DepartmentId { get; set; }

        /// <summary>ID корпуса (Housing_id).</summary>
        [JsonPropertyName("housingId")]
        public int HousingId { get; set; }

        /// <summary>Номер кабинета (office_number), необязательное, до 255 символов.</summary>
        [JsonPropertyName("officeNumber")]
        public string? OfficeNumber { get; set; }

        /// <summary>Тип машины (MachineType), справочник классификаторов оборудования.</summary>
        [JsonPropertyName("machineType")]
        public int MachineType { get; set; } = 1;

        /// <summary>MAC-адрес основного адаптера (ключ уникальности машины на сервере).</summary>
        [JsonPropertyName("macAddress")]
        public string MacAddress { get; set; } = string.Empty;

        /// <summary>Список IP-адресов со всех сетевых адаптеров.</summary>
        [JsonPropertyName("ipAddresses")]
        public List<string> IpAddresses { get; set; } = new();

        /// <summary>Внутренний ID машины на сервере (получен из ответа Sync).</summary>
        [JsonPropertyName("serverMachineId")]
        public int ServerMachineId { get; set; }

        /// <summary>Версия записи (оптимистичный контроль параллелизма).</summary>
        [JsonPropertyName("concurrencyStamp")]
        public int ConcurrencyStamp { get; set; }

        /// <summary>Профиль заполнен достаточно для регистрации на сервере.</summary>
        [JsonIgnore]
        public bool IsComplete =>
            DepartmentId > 0 &&
            HousingId > 0 &&
            MachineType > 0 &&
            MacAddressValidator.IsValid(MacAddress) &&
            IpAddresses.Count > 0;

        /// <summary>Глубокая копия (для сравнения изменений).</summary>
        public MachineProfile Clone() => new()
        {
            DepartmentId = DepartmentId,
            HousingId = HousingId,
            OfficeNumber = OfficeNumber,
            MachineType = MachineType,
            MacAddress = MacAddress,
            IpAddresses = new List<string>(IpAddresses),
            ServerMachineId = ServerMachineId,
            ConcurrencyStamp = ConcurrencyStamp,
        };

        /// <summary>Сравнение значимой части профиля (без серверных полей).</summary>
        public bool SameRegistrationData(MachineProfile other) =>
            DepartmentId == other.DepartmentId &&
            HousingId == other.HousingId &&
            MachineType == other.MachineType &&
            MacAddress == other.MacAddress &&
            (OfficeNumber ?? string.Empty) == (other.OfficeNumber ?? string.Empty) &&
            IpAddresses.SequenceEqual(other.IpAddresses);
    }
}
