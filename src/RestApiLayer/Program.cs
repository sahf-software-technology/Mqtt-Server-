var builder = WebApplication.CreateBuilder(args);

// Add Dapr integration to controllers
builder.Services.AddControllers().AddDapr();

// Add Swagger/OpenAPI for easy testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "IoT REST API with MQTT", 
        Version = "v1",
        Description = "REST API for IoT device management using Dapr and MQTT"
    });
});

// Register Dapr client
builder.Services.AddDaprClient();

// Add CORS if you need it for a frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Always enable Swagger (useful for testing)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// Required for Dapr pub/sub (CloudEvents format)
app.UseCloudEvents();

// Map your controllers
app.MapControllers();

// Required for Dapr to discover topic subscriptions
app.MapSubscribeHandler();

app.Run();