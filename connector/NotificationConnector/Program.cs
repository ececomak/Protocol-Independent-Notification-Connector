using NotificationConnector;
using NotificationConnector.Core;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnector, ConnectorCore>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();