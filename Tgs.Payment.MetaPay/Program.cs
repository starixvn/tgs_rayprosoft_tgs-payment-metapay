using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Payment.MetaPay;
using Tgs.Payment.MetaPay.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
            fluentOptions.UseMessagePack();
        });
    });

builder.Services
    .AddMetaPay(
        options => builder.Configuration.GetSection("MetaPay").Bind(options),
        clientBuilder => clientBuilder
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<MetaPayOptions>>().Value;

                http.BaseAddress = options.PaymentApiUrl;
            }).AddLoggingHandler<MetaPayOptions>())
    .AddPaymentClient()
    .AddPaymentServiceClient(httpBuilder => httpBuilder
        .ConfigureHttpClient((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<MetaPayOptions>>().Value;

            http.BaseAddress = options.PaymentServiceBaseUrl;
        }));

builder.Services.AddMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((options, _) => options.Url =
            builder.Configuration.GetConnectionString("Nats")!)
        .AddProcessor(
            ProcessorTypes.DepositProcessorType,
            "tgs.payment.metapay.deposit",
            "tgs.payment.metapay.deposit")
        .AddProcessor(
            ProcessorTypes.WithdrawProcessorType,
            "tgs.payment.metapay.withdraw",
            "tgs.payment.metapay.withdraw"))
    .AddNatsGlobPatternExchange("*");

builder.Services.AddControllers();

builder.Services
    .AddProfileViewer()
    .AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
        logging.MediaTypeOptions.AddText("application/x-www-form-urlencoded");
        logging.MediaTypeOptions.AddText("text/html");
        logging.MediaTypeOptions.AddText("application/json");
        logging.MediaTypeOptions.AddText("multipart/form-data");
    })
    .AddOpenTelemetry()
    .ConfigureResource(_ => ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
    .WithTracing(tracerProviderBuilder => tracerProviderBuilder
        .AddSource("Tgs.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .WithMetrics(meterProviderBuilder => meterProviderBuilder
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .Services
    .Configure<AspNetCoreInstrumentationOptions>(options =>
        options.Filter = httpContext
            => !httpContext.Request.Path.HasValue
               || !httpContext.Request.Path.StartsWithSegments("/swagger")
               && !httpContext.Request.Path.StartsWithSegments("/healthz")
               && !httpContext.Request.Path.StartsWithSegments("/metrics")
               && !new[] { "html", "ico", "png", "jpg" }.Any(
                   ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpLogging();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Use(async (context, next) =>
{
    await next().ConfigureAwait(false);

    var logger = context.RequestServices.GetService<ILoggerFactory>()
        ?.CreateLogger("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");

    if (logger?.IsEnabled(LogLevel.Information) == true)
    {
        using var stream = new StreamReader(context.Request.Body);

        _ = await stream.ReadToEndAsync().ConfigureAwait(false);
    }
});

app.Run();