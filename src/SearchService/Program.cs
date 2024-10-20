using System.Net;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Models;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

// Add MassTransit service and configure for RabbitMq (Event Bus)
builder.Services.AddMassTransit(x =>
    {
        x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();

        x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));

        x.UsingRabbitMq((context, cfg) =>
        {
            // Retry to consume so it handle transient failures
            cfg.ReceiveEndpoint("search-auction-created", e =>
            {
                e.UseMessageRetry(r => r.Interval(5, 5));

                e.ConfigureConsumer<AuctionCreatedConsumer>(context);
            });

            cfg.ConfigureEndpoints(context);
        });
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await DbInitializer.InitDb(app);
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
});

app.Run();

// Retry policy for database operations until the database is available
static IAsyncPolicy<HttpResponseMessage> GetPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
        .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));