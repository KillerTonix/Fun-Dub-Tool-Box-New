using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FunDub.UI.Services
{
    public class FFmpegService
    {
        public event Action<string>? OnProgressReceived;

        public async Task RunCommandAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = arguments,
                RedirectStandardError = true, // FFmpeg outputs progress to Error stream
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.ErrorDataReceived += (s, e) => {
                if (!string.IsNullOrEmpty(e.Data)) OnProgressReceived?.Invoke(e.Data);
            };

            process.Start();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
        }
    }
}
