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

// --- START: SignalR Service Registration (Fix for InvalidOperationException) ---
// This registers the necessary SignalR services for IHubContext injection.
builder.Services.AddSignalR();
// --- END: SignalR Service Registration ---

// Add CORS if you need it for a frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Allow the React/HTML frontend (running on a different port/origin) to connect
        // Note: For production, specify exact origins instead of AllowAnyOrigin()
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Always enable Swagger (useful for testing)
app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS policy
app.UseCors("AllowAll");

// Required for Dapr pub/sub (CloudEvents format)
app.UseCloudEvents();

// Map your controllers
app.MapControllers();

// --- START: SignalR Hub Endpoint Mapping (Fix for InvalidOperationException) ---
// This maps the hub to the endpoint the frontend is configured to use: /printerhub
app.MapHub<RestApiLayer.Hubs.PrinterHub>("/printerhub");
// --- END: SignalR Hub Endpoint Mapping ---

// Required for Dapr to discover topic subscriptions
app.MapSubscribeHandler();

app.Run();
