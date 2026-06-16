using ActionItems.Sdk.DependencyInjection;
using ActionItems.Sdk.Sharding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "ActionItems Sharding POC", Version = "v1" });
});
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddActionItemsSdk(builder.Configuration);

var app = builder.Build();

await ShardDatabaseInitializer.InitializeAsync(app.Configuration);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ActionItems Sharding POC v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
