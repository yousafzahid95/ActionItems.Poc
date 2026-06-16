using ActionItems.Sdk.DependencyInjection;
using ActionItems.Sdk.Sharding;
using ActionItems.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddActionItemsSdk(builder.Configuration);
builder.Services.AddHostedService<EventConsumerWorker>();

var host = builder.Build();

await ShardDatabaseInitializer.InitializeAsync(host.Services.GetRequiredService<IConfiguration>());

host.Run();
