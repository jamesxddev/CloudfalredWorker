using System.Diagnostics;
using System.Net.Http;

namespace CloudFlaredWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _urlToCheck = "http://192.168.100.27:3001/info"; // Replace with your actual URL

        private readonly HttpClient _httpClient = new();
        private Process? _cloudflaredProcess;
        private bool _cloudflaredRunning = false;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
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
            var certPath = Path.Combine(AppContext.BaseDirectory, "Tools", "cert.pem");
            var credPath = Path.Combine(AppContext.BaseDirectory, "Tools", "75a93ea8-5c49-490a-b1ef-547010e567e2.json");
            var configPath = Path.Combine(AppContext.BaseDirectory, "Tools", "config.yml");

            if (!File.Exists(exePath))
            {
                _logger.LogError($"cloudflared.exe not found at {exePath}");
                return;
            }

            if (!File.Exists(certPath))
            {
                _logger.LogError($"cert.pem not found at {certPath}");
                return;
            }

            if (!File.Exists(credPath))
            {
                _logger.LogError($"credential not found at {credPath}");
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
                    //Arguments = $"--origincert \"{certPath}\" tunnel run malayanprints-tunnel",
                    //Arguments = $"--origincert \"{certPath}\" tunnel run --credentials-file \"{credPath}\" malayanprints-tunnel",
                    Arguments = $"--config \"{configPath}\" tunnel run",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _cloudflaredProcess.OutputDataReceived += (s, e) => _logger.LogInformation(e.Data);
            _cloudflaredProcess.ErrorDataReceived += (s, e) => _logger.LogError(e.Data);

            _cloudflaredProcess.Start();
            _cloudflaredProcess.BeginOutputReadLine();
            _cloudflaredProcess.BeginErrorReadLine();

            _logger.LogInformation("cloudflared started.");
        }

        private async Task<bool> IsWebsiteUpAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_urlToCheck);
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
