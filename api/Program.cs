using FifteenthStandard.LogInWithTwitter;
using FifteenthStandard.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IKeyValueStore, InMemoryKeyValueStore>();

var oauth1aConfig = builder.Configuration.GetSection("OAuth1aConfig").Get<OAuth1aConfig>();
if (oauth1aConfig == null) throw new Exception("OAuth1aConfig missing");
builder.Services.AddSingleton(oauth1aConfig);
builder.Services.AddScoped<OAuth1aService>();

var oauth2Config = builder.Configuration.GetSection("OAuth2Config").Get<OAuth2Config>();
if (oauth2Config == null) throw new Exception("OAuth2Config missing");
builder.Services.AddSingleton(oauth2Config);
builder.Services.AddScoped<OAuth2Service>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#if !DEBUG
app.UseHttpsRedirection();
#endif

app.UseAuthorization();

app.UseCors(policy =>
{
    policy
        .WithOrigins(builder.Configuration["CorsOrigin"] ?? "*")
        .AllowAnyHeader()
        .AllowAnyMethod();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
