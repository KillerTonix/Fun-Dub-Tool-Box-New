using System.IO;
using System.Text.Json;

namespace FunDub.UI.Services
{
    /// <summary>
    /// Persists user preferences to %AppData%/FunDubToolBox/settings.json
    /// </summary>
    public class UserSettings
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FunDubToolBox");

        private static readonly string SettingsPath =
            Path.Combine(SettingsDir, "settings.json");

        // ── Rendering Engine ────────────────────────────────────────
        public int RenderingEngineIndex { get; set; } = 0;

        // ── Saved Material Paths (💾 buttons) ───────────────────────
        public string? SavedIntroPath { get; set; }
        public string? SavedLogoPath { get; set; }
        public string? SavedOutroPath { get; set; }

        // ── Logo Settings ───────────────────────────────────────────
        public double SavedLogoOpacity { get; set; } = 1.0;
        public int SavedLogoPositionIndex { get; set; } = 1; // 0=TL 1=TR 2=BL 3=BR 4=Manual

        // ── Load / Save ─────────────────────────────────────────────
        public static UserSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                }
            }
            catch { }

            return new UserSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
