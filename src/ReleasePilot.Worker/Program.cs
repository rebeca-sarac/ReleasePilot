using ReleasePilot.Infrastructure;
using ReleasePilot.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AuditLogConsumer>();

var host = builder.Build();
host.Run();
