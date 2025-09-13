using System.Text;
using datopus.Api.Endpoints;
using datopus.Application.Converters.Google;
using datopus.Application.Middlewares;
using datopus.Application.Services;
using datopus.Application.Services.Google;
using datopus.Application.Services.Subscriptions;
using datopus.Core.Entities.BigQuery;
using datopus.Core.Services.BigQuery;
using datopus.Core.Services.Imaging;
using datopus.Core.Services.Subscription;
using datopus.payments.Api.Endpoints;
using dotenv.net;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.IdentityModel.Tokens;
using Stripe;
using Stripe.Tax;

#if DEBUG
DotEnv.Load();
#endif

var builder = WebApplication.CreateBuilder(args);

StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_PRIVATE_KEY");

builder
    .Services.AddSingleton(
        (provider) =>
            new Supabase.Client(
                Environment.GetEnvironmentVariable("SUPABASE_URL")!,
                Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")!,
                new Supabase.SupabaseOptions { AutoRefreshToken = true }
            )
    )
    .AddSingleton<DbService>()
    .AddSingleton<AuthService>()
    .AddSingleton(
        (provider) =>
            new AzureBlobService(
                Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")!,
                Environment.GetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME")!
            )
    )
    .AddSingleton<IImageConverter, ImageConvertor>()
    .AddSingleton<ProfileImageService>()
    .AddSingleton(
        (provider) =>
            new GoogleOAuthService(
                Environment.GetEnvironmentVariable("GOOGLE_AUTH_CLIENT_ID")!,
                Environment.GetEnvironmentVariable("GOOGLE_OAUTH_CLIENT_SECRET")!
            )
    )
    .AddSingleton<BQService>()
    .AddSingleton<BQDashboardService>()
    .AddSingleton<SubscriptionDBService>()
    .AddSingleton<BQBuilder, GoogleSqlBQBuilderService>()
    .AddSingleton<UserSubscriptionService>()
    .AddSingleton<Stripe.Checkout.SessionService>()
    .AddSingleton<SubscriptionService>()
    .AddSingleton<CalculationService>()
    .AddSingleton<ProductService>()
    .AddSingleton<PriceService>()
    .AddSingleton<InvoiceService>()
    .AddSingleton<Stripe.BillingPortal.SessionService>()
    .AddSingleton<IPlanService, datopus.Application.Services.Subscriptions.PlanService>();

builder.Services.AddSingleton<IGoogleCredentialProvider, GoogleCredentialProvider>();
builder.Services.AddSingleton<IGoogleBlobStorageService>(provider =>
{
    var credentialProvider = provider.GetRequiredService<IGoogleCredentialProvider>();
    var logger = provider.GetRequiredService<ILogger<GcsBlobStorageService>>();
    var bucketName = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_RRWEB_BUCKET_NAME")!;

    return new GcsBlobStorageService(credentialProvider, bucketName, logger);
});
builder.Services.AddHttpClient();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddCors();
builder.Services.AddAntiforgery();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new BQMatchTypeConverter());
    options.SerializerOptions.Converters.Add(new BQOperationConverter());
});

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")!)
            ),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<JsonExceptionHandlingMiddleware>();

await app.Services.GetRequiredService<Supabase.Client>().InitializeAsync();

ConfigEndpoints.Register(app);
SignupEndpoints.Register(app);
UserProfileEndpoints.Register(app);
GoogleAuthEndpoints.Register(app);
BigQueryEndpoints.Register(app);
AdminEndpoints.Register(app);
SubscriptionEndpoints.Register(app);
CaptureEndpoints.Register(app);
SupportEndpoints.Register(app);

app.Run();
