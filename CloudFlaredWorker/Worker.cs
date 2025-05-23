using CloudFlaredWorker.Model;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;

namespace CloudFlaredWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly CloudflareSettings _settings;

        private readonly HttpClient _httpClient = new();
        private Process? _cloudflaredProcess;
        private bool _cloudflaredRunning = false;

        public Worker(ILogger<Worker> logger, IOptions<CloudflareSettings> options)
        {
            _logger = logger;
            _settings = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var isWebsiteUp = await IsWebsiteUpAsync();

                if (_cloudflaredRunning)
                {
                    return;
                }

                if (isWebsiteUp)
                {
                    StartCloudflared();
                    _cloudflaredRunning = true;
                    _logger.LogWarning("Cloudfalred is up and running...");
                    return;
                }

                _logger.LogWarning("Website is down. retrying...");

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private void StartCloudflared()
        {
            var exePath = Path.Combine(AppContext.BaseDirectory, "Tools", "cloudflared.exe");
            var configPath = Path.Combine(AppContext.BaseDirectory, "Tools", "config.yml");

            if (!File.Exists(exePath))
            {
                _logger.LogError($"cloudflared.exe not found at {exePath}");
                return;
            }

            if (!File.Exists(configPath))
            {
                _logger.LogError($"config.yml not found at {configPath}");
                return;
            }

            _cloudflaredProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--config \"{configPath}\" tunnel run",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // disable cloudflared logs
            //_cloudflaredProcess.OutputDataReceived += (s, e) => _logger.LogInformation(e.Data);
            //_cloudflaredProcess.ErrorDataReceived += (s, e) => _logger.LogError(e.Data);

            _cloudflaredProcess.Start();
            _cloudflaredProcess.BeginOutputReadLine();
            _cloudflaredProcess.BeginErrorReadLine();

            _logger.LogInformation("cloudflared started.");
        }

        private async Task<bool> IsWebsiteUpAsync()
        {
            try
            {
                string url = _settings.UrlToCheck;
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking website: {ex.Message}");
                return false;
            }
        }
    }
}
