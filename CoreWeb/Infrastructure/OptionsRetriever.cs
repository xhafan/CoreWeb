namespace MrWatchdog.Web.Infrastructure;

public static class OptionsRetriever
{
    public static TOptions Retrieve<TOptions>(
        IConfiguration configuration, 
        IServiceCollection services,
        string? configurationSectionKey = null
    )
        where TOptions : class, new()
    {
        var options = new TOptions();
        configurationSectionKey ??= typeof(TOptions).Name.Replace("Options", "");
        var configurationSection = configuration.GetSection(configurationSectionKey);
        services.Configure<TOptions>(configurationSection);
        configurationSection.Bind(options);
        return options;
    }
}