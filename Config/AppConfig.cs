using System.IO;
using System.Text.Json;

namespace BDCOM.OLT.Manager.Config
{
    internal static class AppConfig
    {
        public static string AppName { get; } = "BDCOM OLT Manager";
        public static string Version { get; } = "1.7.4 C# edition";

        // Папка для хранения всех настроек пользователя
        public static string ConfigDir { get; } 
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".olt_manager_pro");

        // Основные файлы
        public static string DevicesFile => Path.Combine(ConfigDir, "devices.json");
        public static string SettingsFile => Path.Combine(ConfigDir, "settings.json");
        public static string LogFile => Path.Combine(ConfigDir, "app.log");

        // Настройки по умолчанию
        public static bool AutoConnect { get; set; } = true;
        public static bool AutoReconnect { get; set; } = true;
        public static int TelnetPort { get; set; } = 23;
        public static int CommandDelay { get; set; } = 800;        // миллисекунды
        public static int ReconnectDelay { get; set; } = 5000;     // миллисекунды
        public static int MaxReconnectAttempts { get; set; } = 3;

        public static void EnsureDirs()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось создать папку конфигурации: {ex.Message}");
            }
        }

        // Загрузка настроек из файла
        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);

                    if (settings != null)
                    {
                        AutoConnect = settings.AutoConnect;
                        AutoReconnect = settings.AutoReconnect;
                        TelnetPort = settings.TelnetPort;
                        CommandDelay = settings.CommandDelay;
                        ReconnectDelay = settings.ReconnectDelay;
                        MaxReconnectAttempts = settings.MaxReconnectAttempts;
                    }
                }
            }
            catch { /* Игнорируем ошибки загрузки настроек */ }
        }

        // Сохранение настроек в файл
        public static void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    AutoConnect = AutoConnect,
                    AutoReconnect = AutoReconnect,
                    TelnetPort = TelnetPort,
                    CommandDelay = CommandDelay,
                    ReconnectDelay = ReconnectDelay,
                    MaxReconnectAttempts = MaxReconnectAttempts
                };

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { /* Игнорируем ошибки сохранения */ }
        }
    }

    // Вспомогательный класс для сериализации настроек
    internal class AppSettings
    {
        public bool AutoConnect { get; set; } = true;
        public bool AutoReconnect { get; set; } = true;
        public int TelnetPort { get; set; } = 23;
        public int CommandDelay { get; set; } = 800;
        public int ReconnectDelay { get; set; } = 5000;
        public int MaxReconnectAttempts { get; set; } = 3;
    }
}