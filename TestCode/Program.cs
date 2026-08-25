using TestCode.Data;
using TestCode.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var mongoSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>();
builder.Services.AddSingleton(mongoSettings);

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<MongoDbInitializer>();
builder.Services.AddSingleton<MongoDbIndexes>();
builder.Services.AddSingleton<TimeEntryService>();

builder.Services.AddScoped<TimeEntryService>();

var app = builder.Build();

app.UseMiddleware<BusinessExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var initializer = app.Services.GetRequiredService<MongoDbInitializer>();
await initializer.InitializeAsync();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
