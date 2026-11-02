using System.Collections.Generic;
using System.Threading.Tasks;
using HelperClient.Models.DTO.ReportDto;

namespace HelperClient.Services
{
    /// <summary>
    /// /HelperAPI/Report — отчёты по IP-адресам.
    /// </summary>
    public class ReportService
    {
        private const string Base = "/Report";
        private readonly ApiClient _api;

        public ReportService(ApiClient api) => _api = api;

        /// <summary>GET /Report/GetAllBusyIP — массив всех занятых IP.</summary>
        public Task<List<string>> GetAllBusyIpAsync() =>
            _api.GetListAsync<string>(Base + "/GetAllBusyIP");

        /// <summary>GET /Report/FoundFreeIP?subnet=&amp;mask= — диапазоны свободных IP (mask ∈ [16,32]).</summary>
        public Task<List<FreeIpRangeDto>> FoundFreeIpAsync(string subnet, int mask)
        {
            Validation.EnsureValidIp(subnet, "subnet");
            Validation.EnsureMaskRange(mask);
            var query = ApiClient.BuildQuery(("subnet", subnet.Trim()), ("mask", mask));
            return _api.GetListAsync<FreeIpRangeDto>(Base + "/FoundFreeIP" + query);
        }

        /// <summary>GET /Report/SearchBusyIp?subnet= — занятые IP по префиксу подсети (например "192.168.1").</summary>
        public Task<List<string>> SearchBusyIpAsync(string subnet)
        {
            Validation.EnsureRequiredText(subnet, "subnet");
            Validation.EnsureValidIpPrefix(subnet, "subnet");
            var query = ApiClient.BuildQuery(("subnet", subnet.Trim()));
            return _api.GetListAsync<string>(Base + "/SearchBusyIp" + query);
        }

        /// <summary>GET /Report/FoundDuplicateIP[?subnet=] — машины с одинаковыми IP.</summary>
        public Task<List<DuplicateIpDto>> FoundDuplicateIpAsync(string? subnet = null)
        {
            if (subnet is not null)
                Validation.EnsureValidIpPrefix(subnet, "subnet");
            var query = ApiClient.BuildQuery(("subnet", subnet?.Trim()));
            return _api.GetListAsync<DuplicateIpDto>(Base + "/FoundDuplicateIP" + query);
        }
    }
}
