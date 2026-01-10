using SaraBank.API.Configurations;
using SaraBank.API.Middleware;

System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

var builder = WebApplication.CreateBuilder(args);

var credentialPath = builder.Configuration["Firestore:CredentialPath"];
if (!string.IsNullOrEmpty(credentialPath))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
}

var projectId = builder.Configuration["Firestore:ProjectId"] ?? "sara-bank";

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen();
builder.Services.AddCustomizedSwagger(typeof(Program));

// Dependency Injection Configuration
builder.Services.AddDependencyInjection(builder.Configuration, projectId);

// Auth no GCP Identity
builder.Services.AddGcpIdentityAuthConfiguration(builder.Configuration, projectId);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCustomizedSwagger();

//app.UseHttpsRedirection();

// Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseGcpIdentityAuthConfiguration();

app.MapControllers();

app.MapGet("/", () => "SaraBank API is Running!");
app.Run();
