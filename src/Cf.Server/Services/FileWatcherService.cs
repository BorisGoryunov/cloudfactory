using Cf.Server.Config;

namespace Cf.Server.Services;

public class FileWatcherService : BackgroundService
{
    private readonly string _watchDirectory;
    private readonly ILogger<FileWatcherService> _logger;
    private FileSystemWatcher? _watcher;

    public FileWatcherService(ILogger<FileWatcherService> logger,
        AppConfig appConfig)
    {
        _watchDirectory = appConfig.BrokerDirectory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(_watchDirectory))
        {
            _logger.LogError("Директория не существует: {Path}", _watchDirectory);
            return;
        }

        _watcher = new FileSystemWatcher(_watchDirectory)
        {
            NotifyFilter = NotifyFilters.FileName,
            Filter = "*.req", 
            IncludeSubdirectories = false 
        };

        _watcher.Created += OnFileCreated;
        _watcher.Error += OnWatcherError;

        _watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Сервис отслеживания файлов запущен. Папка: {Path}", _watchDirectory);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken); 
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Новый файл обнаружен: {FileName}", e.Name);
        ProcessFile(e.FullPath);
    }

    private void ProcessFile(string filePath)
    {
        try
        {
            _logger.LogInformation("Обработка файла: {FilePath}", filePath);
            File.WriteAllText(filePath.Replace("req", "resp"), "200");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке файла {FilePath}", filePath);
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "Ошибка в FileSystemWatcher");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
