# OrleansCounter

This example is inspired by this sample https://github.com/Azure-Samples/build-your-first-orleans-app-aspnetcore. 
Besides the original endpoints it adds two more endpoints, `/counter` for increasing the counter and `/getcounter` for retrieving the current counter. 

### Configuration used to deploy to AKS (choose use reddis as storage)
```c#
// In Kubernetes, we use environment variables and the pod manifest
siloBuilder.UseKubernetesHosting();

// Use Redis for clustering & persistence
var redisConnectionString = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";
siloBuilder.UseRedisClustering(options => options.ConnectionString = redisConnectionString);
siloBuilder.AddRedisGrainStorage("urls", options => options.ConnectionString = redisConnectionString);
```

### Configuration used to deploy to Azure container app (choose to use azure storage account as storage)
```c#
// container app configuration
var connectionString = "azure storage connection string";
siloBuilder
.UseAzureStorageClustering(o => o.ConfigureTableServiceClient(connectionString))
.AddAzureTableGrainStorage("urls", o => o.ConfigureTableServiceClient(connectionString));
```


### Configuration used to deploy to Azure container app (use azure sql server as storage)
```c#
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
```