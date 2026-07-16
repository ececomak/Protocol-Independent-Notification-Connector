using NotificationConnector;
using NotificationConnector.Adapters;
using NotificationConnector.Core;
using NotificationConnector.Options;
using NotificationConnector.Publishers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ConnectorOptions>(
    builder.Configuration.GetSection("Connector")
);

builder.Services.AddSingleton<IConnector, ConnectorCore>();

builder.Services.AddSingleton<MockSourceAdapter>();
builder.Services.AddSingleton<WebhookSourceAdapter>();
builder.Services.AddSingleton<WebSocketSourceAdapter>();

builder.Services.AddHttpClient<INotificationPublisher, BackendNotificationPublisher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();