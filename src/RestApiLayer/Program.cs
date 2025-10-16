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

// --- START: SignalR Service Registration ---
// This registers the necessary SignalR services.
builder.Services.AddSignalR();
// --- END: SignalR Service Registration ---

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // --- START: CRITICAL CORS FIX FOR SignalR ---
        // SignalR requires credentials (AllowCredentials) which conflicts with a simple AllowAnyOrigin().
        // We use SetIsOriginAllowed to effectively allow all origins while satisfying the security requirement.
        policy.SetIsOriginAllowed(_ => true) // Allows any origin
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // REQUIRED by SignalR for client negotiation
        // --- END: CRITICAL CORS FIX FOR SignalR ---
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

// --- START: SignalR Hub Endpoint Mapping ---
// This maps the hub to the endpoint the frontend is configured to use: /printerhub
app.MapHub<RestApiLayer.Hubs.PrinterHub>("/printerhub");
// --- END: SignalR Hub Endpoint Mapping ---

// Required for Dapr to discover topic subscriptions
app.MapSubscribeHandler();

app.Run();
