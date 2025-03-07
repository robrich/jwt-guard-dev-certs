
//
//

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//
// Add services to the container.
//

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

AppSettings appSettings = new AppSettings();
builder.Configuration.Bind("AppSettings", appSettings);
builder.Services.AddSingleton(appSettings);

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

// This is required to be instantiated before the OpenIdConnectOptions starts getting configured.
// By default, the claims mapping will map claim names in the old format to accommodate older SAML applications.
// For instance, 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' instead of 'roles' claim.
// This flag ensures that the ClaimsIdentity claims collection will be built from the claims in the token
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

string issuer = "";
List<string> audiences = new List<string>();
ICollection<SecurityKey> signingKeys = [];
string securityAlgorithm = "";

if (!appSettings.UseDevJwt)
{
    // Microsoft Entra External or Azure B2C
    ArgumentNullException.ThrowIfNull(appSettings.TenantId);
    ArgumentNullException.ThrowIfNull(appSettings.ClientId);
    ArgumentNullException.ThrowIfNull(appSettings.AuthorityDomain);
    string policy = appSettings.SignInPolicyId;

    string authority = $"https://login.microsoftonline.com/tfp/{appSettings.TenantId}/{policy}/v2.0/";
    string openidConfigUrl = $"https://{appSettings.AuthorityDomain}/{appSettings.TenantId}/v2.0/.well-known/openid-configuration?p={policy}";
    Console.WriteLine($"Getting OpenIdConnectConfiguration from: {openidConfigUrl} for authority {authority}");

    ConfigurationManager<OpenIdConnectConfiguration> configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(openidConfigUrl, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever());
    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
    OpenIdConnectConfiguration discoveryDocument = await configurationManager.GetConfigurationAsync();
    issuer = discoveryDocument.Issuer;
    signingKeys = discoveryDocument.SigningKeys;
    audiences.Add(appSettings.ClientId);

    securityAlgorithm = SecurityAlgorithms.RsaSha256;
}
else
{
    // because it reads ValidIssuers[] but writes ValidIssuer: https://github.com/dotnet/aspnetcore/issues/58996
    IdentityModelEventSource.LogCompleteSecurityArtifact = true;
    string? key = builder.Configuration.GetSection("Authentication:Schemes:Bearer:SigningKeys:0").GetValue<string>("Value");
    issuer = builder.Configuration.GetSection("Authentication:Schemes:Bearer:ValidIssuer").Get<string>() ?? "";
    audiences = builder.Configuration.GetSection("Authentication:Schemes:Bearer:ValidAudiences").Get<string[]>()?.ToList() ?? [];
    ArgumentNullException.ThrowIfNullOrWhiteSpace(key, "SigningKeys");
    signingKeys = new List<SecurityKey> { new SymmetricSecurityKey(Convert.FromBase64String(key ?? "")) };
    securityAlgorithm = SecurityAlgorithms.HmacSha256;
}
ArgumentNullException.ThrowIfNullOrWhiteSpace(issuer, "JWT Issuer");
ArgumentNullException.ThrowIfNull(audiences, "JWT Audiences");
ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(audiences.Count, 0, "JWT Audiences");
ArgumentNullException.ThrowIfNull(signingKeys, "JWT Signing Keys");
ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(signingKeys.Count, 0, "JWT Signing Keys");
ArgumentException.ThrowIfNullOrWhiteSpace(securityAlgorithm, "JWT Security Algorithm");

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(jwtOptions => {
    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = issuer,
        ValidIssuers = [issuer],
        //ValidAudience = audience,
        ValidAudiences = audiences,
        IssuerSigningKeys = signingKeys,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        SaveSigninToken = true,
        ValidAlgorithms = [ securityAlgorithm ], // Only trust the right security algorithm depending on the token's source.
        ValidTypes = ["JWT"],
        RequireAudience = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
    };
    jwtOptions.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = JwtAuthenticationFailedLogger.WriteAuthenticationFailed
    };
});

builder.Services.AddControllers(options =>
{
    AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
});

// OpenAPI / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setup =>
{
    // https://stackoverflow.com/questions/43447688/setting-up-swagger-asp-net-core-using-the-authorization-headers-bearer
    OpenApiSecurityScheme jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    setup.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, new List<string>() }
    });
});

// lower-case all URLs:
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);


//
//
WebApplication app = builder.Build();

//
// Configure the HTTP request pipeline.
//

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    // in Linux, must use ASPNETCORE_FORWARDEDHEADERS_ENABLED instead
    // https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?#forward-the-scheme-for-linux-and-non-iis-reverse-proxies
    // trust x-forwarded-for headers
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.All,
        ForwardLimit = null
    });
    app.UseExceptionHandler(builder => builder.Run(GlobalErrorHandler.HandleError));
    app.UseHsts(); // default is 30 days, see https://aka.ms/aspnetcore-hsts
}

app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Not found handler
// FRAGILE: can't listen to all URLs because it blocks magic URLs like /swagger/swagger-ui.css
app.Map("api/{*url}", async (HttpContext context) =>
{
    // log
    IServiceProvider serviceLocator = context.RequestServices;
    ILogger<Program> logger = serviceLocator.GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"404: {context.Request.Path}");
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    await context.Response.WriteAsJsonAsync(new ApiResponse
    {
        Success = false,
        Message = "404: Not found"
    });
});

app.Run();

// for testing
public partial class Program { }
