using CoreBackend.Features.Jobs.Queries;
using CoreWeb.Features.Shared;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;

namespace CoreWeb.Infrastructure;

public static class CoreWebReinforcedTypingsConfiguration
{
    public static void Configure(ConfigurationBuilder builder)
    {
        builder.Global(global => global
            .UseModules()
            .CamelCaseForProperties()
        );

        var coreBackendTypes = typeof(JobDto).Assembly.GetTypes();
        var coreWebTypes = typeof(CoreWebStimulusControllers).Assembly.GetTypes();

        var typesAsTsInterface = coreBackendTypes.Where(x => x.GetCustomAttributes(typeof(TsInterfaceAttribute), inherit: false).Any())
            .Union(coreWebTypes.Where(x => x.GetCustomAttributes(typeof(TsInterfaceAttribute), inherit: false).Any()));

        builder.ExportAsInterfaces(typesAsTsInterface, config => config
            .WithPublicProperties()
            .AutoI(false)
            .DontIncludeToNamespace()
        );

        var typesAsTsClass = coreBackendTypes
            .Where(x => x.GetCustomAttributes(typeof(TsClassAttribute), inherit: false).Any())
            .Union(coreWebTypes.Where(x => x.GetCustomAttributes(typeof(TsClassAttribute), inherit: false).Any()));
        
        builder.ExportAsClasses(typesAsTsClass, config => config
            .WithPublicProperties()
            .DontIncludeToNamespace()
        );

        var enumTypes = coreBackendTypes.Where(x => x.IsEnum && x.IsPublic)
            .Union(coreWebTypes.Where(x => x.IsEnum && x.IsPublic));

        builder.ExportAsEnums(enumTypes, config => config
            .DontIncludeToNamespace()
        );        
    }
}