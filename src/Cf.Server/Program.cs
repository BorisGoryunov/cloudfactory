using Cf.Server;
using Cf.Server.Config;
using Cf.Server.Interfaces;
using Cf.Server.MiddleWare;
using Cf.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var brokerConfig = builder.Configuration
    .GetRequiredSection(nameof(BrokerConfig))
    .Get<BrokerConfig>()!;

builder.Services.AddSingleton(brokerConfig);

var appConfig = builder.Configuration
    .GetRequiredSection(nameof(AppConfig))
    .Get<AppConfig>()!;

builder.Services.AddSingleton(appConfig);

builder.Services.AddHostedService<FileWatcherService>();

builder.Services.AddSingleton<IBrokerService, FileBrokerService>();
builder.Services.AddSingleton<IRequestCollapser, RequestCollapser>();
builder.Services.AddSingleton<HttpProcessingService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

var brokerService = app.Services.GetRequiredService<IBrokerService>();
brokerService.Initialize();

app.UseMiddleware<BrokerMiddleware>();

app.MapGet("/", () => "Broker Gateway Service");

app.MapControllers();

app.Run();
