using System.Diagnostics;
using Cf.Server.Config;
using Cf.Server.Models;
using Cf.Server.Services;

namespace Cf.Server.MiddleWare;

public class BrokerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpProcessingService _processingService;
    private readonly AppConfig _appConfig;
    private readonly ILogger<BrokerMiddleware> _logger;

    public BrokerMiddleware(RequestDelegate next,
        HttpProcessingService processingService,
        AppConfig appConfig,
        ILogger<BrokerMiddleware> logger)
    {
        _next = next;
        _processingService = processingService;
        _appConfig = appConfig;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = new Stopwatch();
        sw.Start();
        try
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                await ProcessBrokerRequest(context);
            }
            else
            {
                await _next(context);
            }
        }
        finally
        {
            sw.Stop();
            var ts = sw.Elapsed;
            _logger.LogTrace($"Elapsed Time: {ts.TotalMilliseconds} ms");
        }
    }

    private async Task ProcessBrokerRequest(HttpContext context)
    {
        var brokerRequest = await CreateBrokerRequest(context.Request);
        try
        {
            BrokerResponse brokerResponse;
            if (_appConfig.UseAdvancedMode)
            {
                brokerResponse =
                    await _processingService.ProcessRequestAdvanced(brokerRequest, context.RequestAborted);
            }
            else
            {
                brokerResponse =
                    await _processingService.ProcessRequestPrimitive(brokerRequest, context.RequestAborted);
            }

            await WriteResponse(context, brokerResponse);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
        }
        catch (TimeoutException)
        {
            context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync($"Broker error: {ex.Message}");
        }
    }

    private async Task<BrokerRequest> CreateBrokerRequest(HttpRequest httpRequest)
    {
        using var reader = new StreamReader(httpRequest.Body);
        var body = await reader.ReadToEndAsync();

        return new BrokerRequest
        {
            Method = httpRequest.Method,
            Path = httpRequest.Path + httpRequest.QueryString,
            Body = body,
            Headers = httpRequest.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
        };
    }

    private async Task WriteResponse(HttpContext context, BrokerResponse brokerResponse)
    {
        context.Response.StatusCode = brokerResponse.StatusCode;

        if (!string.IsNullOrEmpty(brokerResponse.Body))
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(brokerResponse.Body);
        }
    }
}
