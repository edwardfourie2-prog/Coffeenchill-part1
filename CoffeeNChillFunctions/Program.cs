using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Code Attribution for RESTful APIs
//
//
//
//

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// connection string for the Azurite table storage and docker
var connectionString = builder.Configuration["AzureWebJobsStorage"]
                       ?? "UseDevelopmentStorage=true";

builder.Services.AddSingleton(new TableServiceClient(connectionString));

builder.Build().Run();