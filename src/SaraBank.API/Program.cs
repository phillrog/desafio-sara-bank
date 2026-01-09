using SaraBank.API.Configurations;
using SaraBank.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

var credentialPath = builder.Configuration["Firestore:CredentialPath"];
if (!string.IsNullOrEmpty(credentialPath))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
}

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen();
builder.Services.AddCustomizedSwagger(typeof(Program));

// Dependency Injection Configuration
builder.Services.AddDependencyInjection(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCustomizedSwagger();

app.UseHttpsRedirection();

// Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();


app.Run();
