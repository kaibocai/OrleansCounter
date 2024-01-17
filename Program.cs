using Orleans.Configuration;
using Orleans.Runtime;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans((ctx, siloBuilder) =>
{
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        siloBuilder.UseLocalhostClustering();
        siloBuilder.AddMemoryGrainStorage("urls");
    }
    else
    {
        // aks configuration
        //// In Kubernetes, we use environment variables and the pod manifest
        //siloBuilder.UseKubernetesHosting();
        //// Use Redis for clustering & persistence
        //var redisConnectionString = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";
        //siloBuilder.UseRedisClustering(options => options.ConnectionString = redisConnectionString);
        //siloBuilder.AddRedisGrainStorage("urls", options => options.ConnectionString = redisConnectionString);


        // container app configuration
        //var connectionString = "azure storage connection string";
        //siloBuilder
        //.UseAzureStorageClustering(o => o.ConfigureTableServiceClient(connectionString))
        //.AddAzureTableGrainStorage("urls", o => o.ConfigureTableServiceClient(connectionString));

        //siloBuilder.Configure<ClusterOptions>(options =>
        //{
        //    options.ClusterId = "urls-counter";
        //    options.ServiceId = "urls";
        //});

        // Use ADO.NET
        var invariant = "System.Data.SqlClient";
        var connectionString = "sql server connection string";
        // Use ADO.NET for clustering
        siloBuilder.UseAdoNetClustering(options =>
        {
            options.Invariant = invariant;
            options.ConnectionString = connectionString;
        });
        // Use ADO.NET for reminder service
        siloBuilder.UseAdoNetReminderService(options =>
        {
            options.Invariant = invariant;
            options.ConnectionString = connectionString;
        });
        // Use ADO.NET for persistence
        siloBuilder.AddAdoNetGrainStorage("urls", options =>
        {
            options.Invariant = invariant;
            options.ConnectionString = connectionString;
        });
    }
});

var app = builder.Build();

app.MapGet("/", static () => "Welcome to the URL shortener, powered by Orleans!");

app.MapGet("/shorten",
    static async (IGrainFactory grains, HttpRequest request, string url) =>
    {
        var host = $"{request.Scheme}://{request.Host.Value}";

        // Validate the URL query string.
        if (string.IsNullOrWhiteSpace(url) &&
            Uri.IsWellFormedUriString(url, UriKind.Absolute) is false)
        {
            return Results.BadRequest($"""
                The URL query string is required and needs to be well formed.
                Consider, ${host}/shorten?url=https://www.microsoft.com.
                """);
        }

        // Create a unique, short ID
        var shortenedRouteSegment = Guid.NewGuid().GetHashCode().ToString("X");

        // Create and persist a grain with the shortened ID and full URL
        var shortenerGrain =
            grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

        await shortenerGrain.SetUrl(url);

        // Return the shortened URL for later use
        var resultBuilder = new UriBuilder(host)
        {
            Path = $"/go/{shortenedRouteSegment}"
        };

        return Results.Ok(resultBuilder.Uri);
    });

app.MapGet("/go/{shortenedRouteSegment:required}",
    static async (IGrainFactory grains, string shortenedRouteSegment) =>
    {
        // Retrieve the grain using the shortened ID and url to the original URL
        var shortenerGrain =
            grains.GetGrain<IUrlShortenerGrain>(shortenedRouteSegment);

        var url = await shortenerGrain.GetUrl();

        // Handles missing schemes, defaults to "http://".
        var redirectBuilder = new UriBuilder(url);

        return Results.Redirect(redirectBuilder.Uri.ToString());
    });

app.MapGet("/count",
    static async (IGrainFactory grains, HttpRequest request) =>
    {

        // Create and persist a grain with the shortened ID and full URL
        var counterGrain =
            grains.GetGrain<IUrlShortenerGrain>("counter");

        await counterGrain.AddCount();

        return Results.Ok("Counter increased by 1");
    });

app.MapGet("/getcount",
    static async (IGrainFactory grains, HttpRequest request) =>
    {

        // Create and persist a grain with the shortened ID and full URL
        var counterGrain =
            grains.GetGrain<IUrlShortenerGrain>("counter");
        var count = await counterGrain.GetCount();
        return Results.Ok(count.ToString());
    });

app.Run();

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string fullUrl);

    Task<string> GetUrl();

    Task AddCount();

    Task<int> GetCount();
}