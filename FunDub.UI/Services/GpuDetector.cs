using FunDub.UI.Models;
using System.Diagnostics;

namespace FunDub.UI.Services
{
    /// <summary>
    /// Detects available GPU hardware encoders by querying ffmpeg.
    /// </summary>
    public static class GpuDetector
    {
        private static List<HardwareEncoder>? _cached;

        /// <summary>
        /// Returns all hardware encoders that FFmpeg reports as available.
        /// Always includes Software. Results are cached after first call.
        /// </summary>
        public static List<HardwareEncoder> GetAvailableEncoders()
        {
            if (_cached != null) return _cached;

            var available = new List<HardwareEncoder> { HardwareEncoder.Software };

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-hide_banner -encoders",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return available;

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);

                if (output.Contains("h264_nvenc"))
                    available.Add(HardwareEncoder.Nvenc);
                if (output.Contains("h264_amf"))
                    available.Add(HardwareEncoder.Amf);
                if (output.Contains("h264_qsv"))
                    available.Add(HardwareEncoder.Qsv);
            }
            catch
            {
                // ffmpeg not found — only software available
            }

            _cached = available;
            return available;
        }

        /// <summary>
        /// Returns the best available encoder, preferring GPU over CPU.
        /// Priority: NVENC > AMF > QSV > Software
        /// </summary>
        public static HardwareEncoder GetBestEncoder()
        {
            var available = GetAvailableEncoders();
            if (available.Contains(HardwareEncoder.Nvenc)) return HardwareEncoder.Nvenc;
            if (available.Contains(HardwareEncoder.Amf)) return HardwareEncoder.Amf;
            if (available.Contains(HardwareEncoder.Qsv)) return HardwareEncoder.Qsv;
            return HardwareEncoder.Software;
        }

        /// <summary>
        /// Returns the ffmpeg encoder name for the given HardwareEncoder.
        /// </summary>
        public static string GetEncoderName(HardwareEncoder encoder)
        {
            return encoder switch
            {
                HardwareEncoder.Nvenc => "h264_nvenc",
                HardwareEncoder.Amf => "h264_amf",
                HardwareEncoder.Qsv => "h264_qsv",
                _ => "libx264"
            };
        }

        /// <summary>
        /// Returns a user-friendly display name.
        /// </summary>
        public static string GetDisplayName(HardwareEncoder encoder)
        {
            return encoder switch
            {
                HardwareEncoder.Nvenc => "NVIDIA (NVENC)",
                HardwareEncoder.Amf => "AMD (AMF)",
                HardwareEncoder.Qsv => "Intel (QSV)",
                HardwareEncoder.Auto => "Auto-detect (Best)",
                _ => "CPU (Software)"
            };
        }
    }
}
