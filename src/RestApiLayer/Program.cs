var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// This adds the DaprClient and other services needed for Dapr integration.
builder.Services.AddControllers().AddDapr();

// Add Swagger/OpenAPI for easy testing if you like
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// This middleware is required for Dapr to wrap messages in the CloudEvents format.
app.UseCloudEvents();

app.MapControllers();

// This endpoint is used by the Dapr sidecar to discover which topics to subscribe to.
// It scans for the [Topic] attributes in your controllers.
app.MapSubscribeHandler();

app.Run();