using TransitManager.API.Hubs;


var builder = WebApplication.CreateBuilder(args);


// === DÉBUT DES AJOUTS ===

// 1. Ajouter le service SignalR
builder.Services.AddSignalR();

// 2. Ajouter une politique CORS permissive pour le développement
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true) // Autorise toutes les origines
              .AllowCredentials();
    });
});

// === FIN DES AJOUTS ===


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

// === LIGNE À AJOUTER ===
app.UseCors("AllowAll");
// ======================


app.UseAuthorization();

app.MapControllers();

// === LIGNE À AJOUTER : mapper le Hub à une URL ===
app.MapHub<NotificationHub>("/notificationHub");
// ================================================


app.Run();
