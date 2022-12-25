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
 * Bu kod, bir uygulamanýn ayarlarýndan "JwtSetting" bölümünü alýr ve bu bölümden bir JwtSetting nesnesi oluþturur.
 * Daha sonra, bu nesne kullanýlarak uygulamanýn hizmetlerini yapýlandýrýr.
 * Bu sayede uygulama, JWT (JSON Web Token) kimlik doðrulama mekanizmasýný kullanarak kimlik doðrulama iþlemlerini gerçekleþtirebilir.
 */
#endregion

#region MONGO
var mongoDbConnectionString = builder.Configuration.GetConnectionString("MongoDb");

var client = new MongoClient(mongoDbConnectionString);
var database = client.GetDatabase("LinkShortenerDb");

builder.Services.AddSingleton(database);

/*
 *Bu kod, bir MongoDB veritabanýna baðlantý kurar ve bu baðlantýdan "LinkShortenerDb" adlý bir veritabanýný alýr.
 *Daha sonra bu veritabaný nesnesini bir hizmet olarak ekler ve bu sayede uygulama içinde bu veritabanýna eriþilebilir hale gelir. 
 *Bu veritabaný kullanýlarak veriler saklanabilir, sorgulanabilir ve güncellenebilir.
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
 *Bu kod .NET Core bir uygulamada bir JSON Web Token (JWT) doðrulama parametrelerini ayarlar. 
 *Öncelikle, bir "JwtSettings:Secret" anahtarýnýn deðeri olarak kullanýlan bir dizi dize dönüþtürülür ve ASCII kodlamasý kullanýlarak bir dize dizisine dönüþtürülür.
 *Ardýndan, bu dizi kullanýlarak bir simetrik güvenlik anahtarý oluþturulur. 
 *Ayrýca, "JwtSettings:Issuer" ve "JwtSettings:Audience" anahtarlarýnýn deðerleri kullanýlarak bir emisyon ve bir izleyici ayarlanýr.
 *Son olarak, tüm bu ayarlar kullanýlarak bir TokenValidationParameters nesnesi oluþturulur ve uygulamanýn hizmetlerine eklenir.
 *Bu nesne, bir JWT'nin geçerli bir emisyon ve izleyiciye sahip olup olmadýðýný doðrulamak için kullanýlabilir.
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
 *Bu kod bir ASP.NET Core uygulamasýnda bir güvenlik ayarý oluþturmak için kullanýlýr.
 *Özellikle, AddAuthentication ve AddJwtBearer metodlarý kullanýlarak bir JWT (JSON Web Token) kimlik doðrulama saðlamasý oluþturulmaktadýr. 
 *Bu kimlik doðrulama saðlama, belirtilen JwtBearerDefaults.AuthenticationScheme deðerini kullanarak varsayýlan bir doðrulama mekanizmasý olarak ayarlanýr.
 *Ayrýca, jwt.SaveToken ve jwt.TokenValidationParameters gibi ayarlar kullanýlarak JWT'lerin nasýl iþlenmesi gerektiði belirtilir.
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
 *Bu kod, bir ASP.NET Core uygulamasýnda Swagger belgelerini oluþturmak için kullanýlýr.
 *Swagger, bir REST API'sýný tanýmlamak ve kullanýcýlarýna dökümantasyon saðlamak için bir araçtýr.
 *Bu kod AddSwaggerGen metodu kullanýlarak bir Swagger belge oluþturulur ve bu belge için bir güvenlik tanýmý ve güvenlik gereksinimi eklenir.
 *Bu sayede, bu belge sadece belirtilen bir JWT (JSON Web Token) ile eriþilebilir hale gelir.
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