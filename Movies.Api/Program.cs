using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Movies.Api.Auth;
using Movies.Api.Endpoints;
using Movies.Api.Health;
using Movies.Api.Mapping;
using Movies.Api.Swagger;
using Movies.Application;
using Movies.Application.Database;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// authentication & validation
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters() // we're going to choose how to validate the token
    {
        // we have to be really, really careful to not let this key leak
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!)),
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = config["JWT:Issuer"],
        ValidAudience = config["JWT:Audience"],
        ValidateIssuer = true,
        ValidateAudience = true
    };
});

builder.Services.AddAuthorization(x =>
{
    // we're commenting it after providing changes in AdminAuthRequirement
    // x.AddPolicy(AuthConstants.AdminUserPolicyName, p => p
    //     .RequireClaim(AuthConstants.AdminUserClaimName, "true"));

    // that policy relies on requirements now
    x.AddPolicy(AuthConstants.AdminUserClaimName,
        p => p.AddRequirements(new AdminAuthRequirement(config["ApiKey"]!)));

    x.AddPolicy(AuthConstants.TrustedMemberPolicyName, p => p
        .RequireAssertion(c => c.User
            .HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: "true" }
                or { Type: AuthConstants.TrustedMemberClaimName, Value: "true" })));
});

builder.Services.AddScoped<ApiKeyAuthFilter>();

builder.Services.AddApiVersioning(x =>
    {
        x.DefaultApiVersion = new ApiVersion(1.0);
        x.AssumeDefaultVersionWhenUnspecified = true;
        x.ReportApiVersions = true;
        x.ApiVersionReader = new MediaTypeApiVersionReader("api-version");
        // HeaderApiVersionReader
        // With MediaTypeApiVersionReader()
        // Headers: key: Accept, Value: application/json;api-version:1.0
    }) // .AddMvc() // no longer needed in minimal API
    .AddApiExplorer();

builder.Services.AddEndpointsApiExplorer(); // this instead

// builder.Services.AddResponseCaching();
builder.Services.AddOutputCache(x =>
{
    x.AddBasePolicy(c => c.Cache());
    x.AddPolicy("MovieCache", c =>
    {
        c.Cache()
            .Expire(TimeSpan.FromMinutes(1))
            .SetVaryByQuery(new[] { "title", "year", "sortBy", "page", "pageSize" })
            .Tag("movies"); // tagging allows us to invalidate cache entries
    });
});

// builder.Services.AddControllers();

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
// builder.Services.AddEndpointsApiExplorer(); // we can remove it after invoking AddApiExplorer() extension method
builder.Services.AddSwaggerGen(x => x.OperationFilter<SwaggerDefaultValues>());

builder.Services.AddApplication();
builder.Services.AddDatabase(config["Database:ConnectionString"]!);

var app = builder.Build();

app.CreateApiVersionSet();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(x =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            x.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
        }
    });
}

app.MapHealthChecks("_health"); // underscore sort of excludes it from being one of the "official endpoints" and makes it more meta endpoint
// it rather has to do with something about the service rather than the domain of the service

app.UseHttpsRedirection();

app.UseAuthentication(); // don't forget about this one!
app.UseAuthorization();

//app.UseCors(); // very important to use Cors before ResponseCaching or OutputCaching
// middleware is sequential so we're going have that response caching after authentication and authorization
// app.UseResponseCaching();
//app.UseOutputCache();

app.UseMiddleware<ValidationMappingMiddleware>();

// app.MapControllers();
app.MapApiEndpoints();

var dbInitializer = app.Services.GetRequiredService<DbInitializer>();
await dbInitializer.InitializeAsync();

app.Run();