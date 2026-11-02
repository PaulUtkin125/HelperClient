using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using HelperClient.Models.DTO.VersionDto;

namespace HelperClient.Services
{
    /// <summary>
    /// /HelperAPI/Version — версии ПО и файлы для установки/обновления клиента.
    /// </summary>
    public class VersionService
    {
        private const string Base = "/Version";
        private readonly ApiClient _api;
        private readonly AppConfig _config;

        public VersionService(ApiClient api, AppConfig config)
        {
            _api = api;
            _config = config;
        }

        /// <summary>GET /Version/GetAll — список версий (доступен всем).</summary>
        public Task<List<VersionResponseDto>> GetAllAsync() =>
            _api.GetListAsync<VersionResponseDto>(Base + "/GetAll");

        /// <summary>GET /Version/Search?searchString=… — поиск по вхождению подстроки.</summary>
        public Task<List<VersionResponseDto>> SearchAsync(string searchString)
        {
            Validation.EnsureRequiredText(searchString, "searchString");
            var query = ApiClient.BuildQuery(("searchString", searchString.Trim()));
            return _api.GetListAsync<VersionResponseDto>(Base + "/Search" + query);
        }

        /// <summary>Вычисляет SHA-256 хеш файла (64 hex-символа) — значение для поля FileHash.</summary>
        public static async Task<string> ComputeFileHashAsync(string filePath)
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// POST /Version/Add (multipart/form-data): Program_id, Release_dateTime ≤ сегодня,
        /// DownloadFile ≤ 100 МБ (иначе 413), FileHash — 64 символа (несовпадение → 422).
        /// </summary>
        public async Task AddAsync(int programId, DateTime releaseDateTime, bool endPoint, string filePath)
        {
            Validation.EnsurePositiveId(programId, "Program_id");
            Validation.EnsureNotFuture(releaseDateTime, "Release_dateTime");

            var info = new FileInfo(filePath);
            if (!info.Exists)
                throw new FileNotFoundException("Файл версии не найден.", filePath);

            // клиентская проверка лимита 100 МБ до отправки (сервер ответил бы 413)
            var limit = (long)_config.MaxDownloadFileSizeMb * 1024 * 1024;
            if (info.Length > limit)
                throw new ArgumentException($"DownloadFile: размер {info.Length / (1024 * 1024)} МБ превышает лимит {_config.MaxDownloadFileSizeMb} МБ.");

            var fileHash = await ComputeFileHashAsync(filePath);
            Validation.EnsureFileHash(fileHash);

            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(programId.ToString()), "Program_id");
            form.Add(new StringContent(Validation.ToIsoUtc(releaseDateTime)), "Release_dateTime");
            form.Add(new StringContent(endPoint ? "true" : "false"), "End_point");
            form.Add(new StringContent(fileHash), "FileHash");

            var fileContent = new StreamContent(File.OpenRead(filePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "DownloadFile", Path.GetFileName(filePath));

            await _api.PostMultipartAsync(Base + "/Add", form);
        }

        /// <summary>DELETE /Version/Delete — id + ConcurrencyStamp.</summary>
        public Task DeleteAsync(int id, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(id, "id");
            return _api.DeleteAsync(Base + "/Delete",
                new DeleteVersionDto { id = id, ConcurrencyStamp = concurrencyStamp });
        }

        /// <summary>PATCH /Version/UpdateIsEndPoint — обновляет только поле end_point.</summary>
        public Task UpdateIsEndPointAsync(int id, bool endPoint, int concurrencyStamp)
        {
            Validation.EnsurePositiveId(id, "id");
            return _api.PatchAsync(Base + "/UpdateIsEndPoint",
                new UpdateIsEndPointDto { id = id, end_point = endPoint, ConcurrencyStamp = concurrencyStamp });
        }

        /// <summary>
        /// GET /Version/Download?id=… — скачивает файл (application/octet-stream) в указанный путь.
        /// После скачивания проверяет SHA-256; при несовпадении файл удаляется (битый файл).
        /// </summary>
        public async Task DownloadAsync(int id, string destinationPath, string? expectedHash = null)
        {
            Validation.EnsurePositiveId(id, "id");

            var bytes = await _api.DownloadAsync($"{Base}/Download?id={id}");
            await File.WriteAllBytesAsync(destinationPath, bytes);

            if (expectedHash is not null)
            {
                var actual = await ComputeFileHashAsync(destinationPath);
                if (!string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(destinationPath);
                    throw new ApiException(422, new Models.DTO.Common.ProblemDetailsDto
                    {
                        Status = 422,
                        Code = "HASH_MISMATCH",
                        Message = "Хеш скачанного файла не совпадает с ожидаемым — файл повреждён."
                    });
                }
            }
        }
    }
}
