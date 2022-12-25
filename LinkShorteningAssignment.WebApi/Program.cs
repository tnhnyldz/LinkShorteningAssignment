using LinkShorteningAssignment.WebApi.Middlewares;
using LinkShorteningAssignment.WebApi.Models;
using LinkShorteningAssignment.WebApi.Services.AuthenticatedUserService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region JWT
var jwtSettingsSection = builder.Configuration.GetSection("JwtSetting");
var jwtSettings = jwtSettingsSection.Get<JwtSetting>();
builder.Services.Configure<JwtSetting>(jwtSettingsSection);

/*
 * Bu kod, bir uygulaman�n ayarlar�ndan "JwtSetting" b�l�m�n� al�r ve bu b�l�mden bir JwtSetting nesnesi olu�turur.
 * Daha sonra, bu nesne kullan�larak uygulaman�n hizmetlerini yap�land�r�r.
 * Bu sayede uygulama, JWT (JSON Web Token) kimlik do�rulama mekanizmas�n� kullanarak kimlik do�rulama i�lemlerini ger�ekle�tirebilir.
 */
#endregion

#region MONGO
var mongoDbConnectionString = builder.Configuration.GetConnectionString("MongoDb");

var client = new MongoClient(mongoDbConnectionString);
var database = client.GetDatabase("LinkShortenerDb");

builder.Services.AddSingleton(database);

/*
 *Bu kod, bir MongoDB veritaban�na ba�lant� kurar ve bu ba�lant�dan "LinkShortenerDb" adl� bir veritaban�n� al�r.
 *Daha sonra bu veritaban� nesnesini bir hizmet olarak ekler ve bu sayede uygulama i�inde bu veritaban�na eri�ilebilir hale gelir. 
 *Bu veritaban� kullan�larak veriler saklanabilir, sorgulanabilir ve g�ncellenebilir.
 */
#endregion

#region AUTH
var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

var tokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,
    ValidateIssuer = true,
    ValidateLifetime = false,
    ValidateIssuerSigningKey = false,
    ValidIssuer = jwtSettings.Issuer,
    ValidAudience = jwtSettings.Audience,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ClockSkew = TimeSpan.Zero
};
builder.Services.AddSingleton(tokenValidationParameters);
/*
 *Bu kod .NET Core bir uygulamada bir JSON Web Token (JWT) do�rulama parametrelerini ayarlar. 
 *�ncelikle, bir "JwtSettings:Secret" anahtar�n�n de�eri olarak kullan�lan bir dizi dize d�n��t�r�l�r ve ASCII kodlamas� kullan�larak bir dize dizisine d�n��t�r�l�r.
 *Ard�ndan, bu dizi kullan�larak bir simetrik g�venlik anahtar� olu�turulur. 
 *Ayr�ca, "JwtSettings:Issuer" ve "JwtSettings:Audience" anahtarlar�n�n de�erleri kullan�larak bir emisyon ve bir izleyici ayarlan�r.
 *Son olarak, t�m bu ayarlar kullan�larak bir TokenValidationParameters nesnesi olu�turulur ve uygulaman�n hizmetlerine eklenir.
 *Bu nesne, bir JWT'nin ge�erli bir emisyon ve izleyiciye sahip olup olmad���n� do�rulamak i�in kullan�labilir.
 */
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(jwt =>
    {
        jwt.SaveToken = true;
        jwt.TokenValidationParameters = tokenValidationParameters;
    });
/*
 *Bu kod bir ASP.NET Core uygulamas�nda bir g�venlik ayar� olu�turmak i�in kullan�l�r.
 *�zellikle, AddAuthentication ve AddJwtBearer metodlar� kullan�larak bir JWT (JSON Web Token) kimlik do�rulama sa�lamas� olu�turulmaktad�r. 
 *Bu kimlik do�rulama sa�lama, belirtilen JwtBearerDefaults.AuthenticationScheme de�erini kullanarak varsay�lan bir do�rulama mekanizmas� olarak ayarlan�r.
 *Ayr�ca, jwt.SaveToken ve jwt.TokenValidationParameters gibi ayarlar kullan�larak JWT'lerin nas�l i�lenmesi gerekti�i belirtilir.
 */
#endregion

#region OTHERS
builder.Services.AddCors();

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IAuthenticatedUserService, AuthenticatedUserService>();

builder.Services.AddControllers();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
#endregion

#region SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Link Shortening Assignment Api", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
});
/*
 *Bu kod, bir ASP.NET Core uygulamas�nda Swagger belgelerini olu�turmak i�in kullan�l�r.
 *Swagger, bir REST API's�n� tan�mlamak ve kullan�c�lar�na d�k�mantasyon sa�lamak i�in bir ara�t�r.
 *Bu kod AddSwaggerGen metodu kullan�larak bir Swagger belge olu�turulur ve bu belge i�in bir g�venlik tan�m� ve g�venlik gereksinimi eklenir.
 *Bu sayede, bu belge sadece belirtilen bir JWT (JSON Web Token) ile eri�ilebilir hale gelir.
 */
#endregion

#region APP
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
#endregion