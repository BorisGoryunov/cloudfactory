using System.Diagnostics;
using System.Text.Json;
using Cf.Server.Config;
using Cf.Server.Interfaces;
using Cf.Server.Models;

namespace Cf.Server.Services;

public class FileBrokerService : IBrokerService
{
    private readonly BrokerConfig _config;
    private readonly ILogger<FileBrokerService> _logger;
    private readonly AppConfig _appConfig;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private bool _disposed;

    public FileBrokerService(BrokerConfig config,
        ILogger<FileBrokerService> logger,
        AppConfig appConfig)
    {
        _config = config;
        _logger = logger;
        _appConfig = appConfig;
    }

    public void Initialize()
    {
        if (Directory.Exists(_appConfig.BrokerDirectory))
        {
            return;
        }

        Directory.CreateDirectory(_appConfig.BrokerDirectory);

        _logger.LogInformation("Created broker directory: {Directory}", _appConfig.BrokerDirectory);
    }

    public async Task<BrokerResponse> SendRequestAsync(BrokerRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestKey = CalculateRequestKey(request.Method, request.Path);
        var requestFile = Path.Combine(_appConfig.BrokerDirectory, $"{requestKey}.req");
        var responseFile = Path.Combine(_appConfig.BrokerDirectory, $"{requestKey}.resp");

        await CleanupFiles(requestFile, responseFile);

        await WriteRequestFile(requestFile, request);

        return await WaitForResponseAsync(responseFile, requestKey, cancellationToken);
    }

    private string CalculateRequestKey(string method, string path)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var input = $"{method}{path}";
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private async Task WriteRequestFile(string filePath, BrokerRequest request)
    {
        var retryCount = 0;
        while (retryCount < 3)
        {
            try
            {
                var content = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, content);
                return;
            }
            catch (IOException) when (retryCount < 2)
            {
                retryCount++;
                await Task.Delay(100);
            }
        }

        throw new InvalidOperationException($"Failed to write request file: {filePath}");
    }

    private async Task<BrokerResponse> WaitForResponseAsync(string responseFile, string requestKey,
        CancellationToken cancellationToken)
    {
        var timeoutToken = new CancellationTokenSource(_config.RequestTimeoutMs);
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token).Token;

        try
        {
            while (!linkedToken.IsCancellationRequested)
            {
                if (File.Exists(responseFile))
                {
                    return await ReadResponseFile(responseFile);
                }

                await Task.Delay(_config.PollingIntervalMs, linkedToken);
            }

            throw new TimeoutException($"Timeout waiting for response for request: {requestKey}");
        }
        finally
        {
            timeoutToken.Dispose();
            await CleanupFiles(responseFile, responseFile.Replace(".resp", ".req"));
        }
    }

    private async Task<BrokerResponse> ReadResponseFile(string filePath)
    {
        await using var fileStream = new FileStream(filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.SequentialScan);

        using var reader = new StreamReader(fileStream);

        var firstLine = await reader.ReadLineAsync() ?? "500";

        if (!int.TryParse(firstLine, out var statusCode))
        {
            statusCode = 500;
        }

        var body = await reader.ReadToEndAsync();
        return new BrokerResponse { StatusCode = statusCode, Body = body };
    }

    private async Task CleanupFiles(params string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {FilePath}", filePath);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }

        _watchers.Clear();

        _disposed = true;
    }
}
