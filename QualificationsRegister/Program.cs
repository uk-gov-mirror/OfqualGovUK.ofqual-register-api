using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ofqual.Common.RegisterAPI.UseCase;
using Ofqual.Common.RegisterAPI.UseCase.Interfaces;
using Ofqual.Common.RegisterAPI.UseCase.Organisations;
using System.Text.Json;
using Ofqual.Common.RegisterAPI.Services.Database;
using Microsoft.EntityFrameworkCore;
using Ofqual.Common.RegisterAPI.Database;
using Ofqual.Common.RegisterAPI.UseCase.Qualifications;
using Ofqual.Common.RegisterFrontend.RegisterAPI;
using Refit;
using Ofqual.Common.RegisterAPI.UseCase.RecognitionScopes;
using Microsoft.Extensions.Configuration;
using System.Drawing.Text;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        RegisterUseCases(services);

        var connString = GetConfigurationValue(context, "MDDBConnString");

        services.AddDbContext<RegisterDbContext>(
            options =>
            {
                options.UseSqlServer(connString);
            });

        services.AddScoped<IRegisterDb, RegisterDb>();

        services.AddHttpClient();
        services.AddHttpClient("APIMgmt", client =>
        {
            client.BaseAddress = new Uri(GetConfigurationValue(context, "APIMgmtURL")!);
        });

        services.AddRefitClient<IRefDataAPIClient>().ConfigureHttpClient(httpClient =>
        {
            httpClient.BaseAddress = new Uri(GetConfigurationValue(context, "RefDataAPIUrl")!);
        });
    })
    .Build();


host.Run();

static void RegisterUseCases(IServiceCollection services)
{
    services.AddScoped<IGetOrganisationByNumberUseCase, GetOrganisationByNumberUseCase>();
    services.AddScoped<IGetOrganisationsListUseCase, GetOrganisationsListUseCase>();

    services.AddScoped<IGetQualificationByNumberUseCase, GetQualificationByNumberUseCase>();
    services.AddScoped<IGetQualificationsListUseCase, GetQualificationsListUseCase>();

    services.AddScoped<IGetScopesByOrganisationNumberUseCase, GetScopesByOrganisationNumberUseCase>();
}


static string GetConfigurationValue(HostBuilderContext context, string key) =>
        context.Configuration?.GetValue<string>(key) ??
        Environment.GetEnvironmentVariable(key)!;    

