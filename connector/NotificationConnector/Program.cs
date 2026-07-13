using NotificationConnector;
using NotificationConnector.Adapters;
using NotificationConnector.Core;
using NotificationConnector.Publishers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnector, ConnectorCore>();

builder.Services.AddSingleton<MockSourceAdapter>();

builder.Services.AddHttpClient<INotificationPublisher, BackendNotificationPublisher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();