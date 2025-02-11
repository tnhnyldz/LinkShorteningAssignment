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
 * Bu kod, bir uygulamanın ayarlarından "JwtSetting" bölümünü alır ve bu bölümden bir JwtSetting nesnesi oluşturur.
 * Daha sonra, bu nesne kullanılarak uygulamanın hizmetlerini yapılandırır.
 * Bu sayede uygulama, JWT (JSON Web Token) kimlik doğrulama mekanizmasını kullanarak kimlik doğrulama işlemlerini gerçekleştirebilir.
 */
#endregion

#region MONGO
var mongoDbConnectionString = builder.Configuration.GetConnectionString("MongoDb");

var client = new MongoClient(mongoDbConnectionString);
var database = client.GetDatabase("LinkShortenerDb");

builder.Services.AddSingleton(database);

/*
 *Bu kod, bir MongoDB veritabanına bağlantı kurar ve bu bağlantıdan "LinkShortenerDb" adlı bir veritabanını alır.
 *Daha sonra bu veritabanı nesnesini bir hizmet olarak ekler ve bu sayede uygulama içinde bu veritabanına erişilebilir hale gelir. 
 *Bu veritabanı kullanılarak veriler saklanabilir, sorgulanabilir ve güncellenebilir.
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
 *Bu kod .NET Core bir uygulamada bir JSON Web Token (JWT) doğrulama parametrelerini ayarlar. 
 *Öncelikle, bir "JwtSettings:Secret" anahtarının değeri olarak kullanılan bir dizi dize dönüştürülür ve ASCII kodlaması kullanılarak bir dize dizisine dönüştürülür.
 *Ardından, bu dizi kullanılarak bir simetrik güvenlik anahtarı oluşturulur. 
 *Ayrıca, "JwtSettings:Issuer" ve "JwtSettings:Audience" anahtarlarının değerleri kullanılarak bir emisyon ve bir izleyici ayarlanır.
 *Son olarak, tüm bu ayarlar kullanılarak bir TokenValidationParameters nesnesi oluşturulur ve uygulamanın hizmetlerine eklenir.
 *Bu nesne, bir JWT'nin geçerli bir emisyon ve izleyiciye sahip olup olmadığını doğrulamak için kullanılabilir.
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
 *Bu kod bir ASP.NET Core uygulamasında bir güvenlik ayarı oluşturmak için kullanılır.
 *Özellikle, AddAuthentication ve AddJwtBearer metodları kullanılarak bir JWT (JSON Web Token) kimlik doğrulama sağlaması oluşturulmaktadır. 
 *Bu kimlik doğrulama sağlama, belirtilen JwtBearerDefaults.AuthenticationScheme değerini kullanarak varsayılan bir doğrulama mekanizması olarak ayarlanır.
 *Ayrıca, jwt.SaveToken ve jwt.TokenValidationParameters gibi ayarlar kullanılarak JWT'lerin nasıl işlenmesi gerektiği belirtilir.
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
 *Bu kod, bir ASP.NET Core uygulamasında Swagger belgelerini oluşturmak için kullanılır.
 *Swagger, bir REST API'sını tanımlamak ve kullanıcılarına dökümantasyon sağlamak için bir araçtır.
 *Bu kod AddSwaggerGen metodu kullanılarak bir Swagger belge oluşturulur ve bu belge için bir güvenlik tanımı ve güvenlik gereksinimi eklenir.
 *Bu sayede, bu belge sadece belirtilen bir JWT (JSON Web Token) ile erişilebilir hale gelir.
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