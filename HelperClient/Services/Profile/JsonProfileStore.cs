using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HelperClient.Models.Profile;

namespace HelperClient.Services.Profile
{
    /// <summary>
    /// Локальное хранилище профиля машины (файл конфигурации).
    /// Кроссплатформенно: Windows — %LocalAppData%\HelperClient\profile.json,
    /// Linux — $XDG_CONFIG_HOME|~/.config/HelperClient/profile.json.
    /// </summary>
    public interface IProfileStore
    {
        string FilePath { get; }

        /// <summary>Профиль существует на диске (первичная настройка не требуется).</summary>
        bool Exists { get; }

        MachineProfile Load();

        void Save(MachineProfile profile);

        Task<MachineProfile> LoadAsync(CancellationToken ct = default);

        Task SaveAsync(MachineProfile profile, CancellationToken ct = default);
    }

    public sealed class JsonProfileStore : IProfileStore
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly MachineProfile _fallback;

        public JsonProfileStore(string? directory = null)
        {
            var dir = directory ?? GetDefaultConfigDirectory();
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "profile.json");
            _fallback = new MachineProfile();
        }

        public string FilePath { get; }

        public bool Exists => File.Exists(FilePath);

        public MachineProfile Load()
        {
            _gate.Wait();
            try
            {
                return ReadUnsafe();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<MachineProfile> LoadAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!File.Exists(FilePath)) return _fallback.Clone();
                await using var fs = File.OpenRead(FilePath);
                var dto = await JsonSerializer
                    .DeserializeAsync<MachineProfile>(fs, Options, ct)
                    .ConfigureAwait(false);
                return dto ?? _fallback.Clone();
            }
            catch (JsonException)
            {
                // Повреждённый файл конфига не должен ронять клиента — стартуем с пустого профиля.
                return _fallback.Clone();
            }
            catch (IOException)
            {
                return _fallback.Clone();
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Save(MachineProfile profile)
        {
            _gate.Wait();
            try
            {
                WriteUnsafe(profile);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task SaveAsync(MachineProfile profile, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(profile);
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var dir = Path.GetDirectoryName(FilePath)!;
                Directory.CreateDirectory(dir);
                var tmp = FilePath + ".tmp";
                await using (var fs = File.Create(tmp))
                {
                    await JsonSerializer.SerializeAsync(fs, profile, Options, ct).ConfigureAwait(false);
                }

                File.Move(tmp, FilePath, overwrite: true);
            }
            finally
            {
                _gate.Release();
            }
        }

        private MachineProfile ReadUnsafe()
        {
            if (!File.Exists(FilePath)) return _fallback.Clone();
            try
            {
                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json)) return _fallback.Clone();
                return JsonSerializer.Deserialize<MachineProfile>(json, Options) ?? _fallback.Clone();
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                return _fallback.Clone();
            }
        }

        private void WriteUnsafe(MachineProfile profile)
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(profile, Options));
            File.Move(tmp, FilePath, overwrite: true);
        }

        /// <summary>Каталог конфигурации пользователя для текущей ОС.</summary>
        public static string GetDefaultConfigDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
            {
                // Fallback для окружений без LocalApplicationData (редкие Linux-контейнеры).
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                appData = Path.Combine(home, ".config");
            }

            return Path.Combine(appData, "HelperClient");
        }
    }
}
