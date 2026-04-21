using CoreBackend.Account.Features.LoginLink.Domain;
using CoreWeb.Account.Features.LoginLink;
using Reinforced.Typings.Attributes;
using Reinforced.Typings.Fluent;

namespace CoreWeb.Account.Infrastructure;

public static class CoreWebAccountReinforcedTypingsConfiguration
{
    public static void Configure(ConfigurationBuilder builder)
    {
        builder.Global(global => global
            .UseModules()
            .CamelCaseForProperties()
        );

        var coreBackendAccountTypes = typeof(LoginTokenDto).Assembly.GetTypes();
        var coreWebAccountTypes = typeof(CoreWebAccountUrlConstants).Assembly.GetTypes();

        var typesAsTsInterface = coreBackendAccountTypes.Where(x => x.GetCustomAttributes(typeof(TsInterfaceAttribute), inherit: false).Any())
            .Union(coreWebAccountTypes.Where(x => x.GetCustomAttributes(typeof(TsInterfaceAttribute), inherit: false).Any()));

        builder.ExportAsInterfaces(typesAsTsInterface, config => config
            .WithPublicProperties()
            .AutoI(false)
            .DontIncludeToNamespace()
        );

        var typesAsTsClass = coreBackendAccountTypes
            .Where(x => x.GetCustomAttributes(typeof(TsClassAttribute), inherit: false).Any())
            .Union(coreWebAccountTypes.Where(x => x.GetCustomAttributes(typeof(TsClassAttribute), inherit: false).Any()));
        
        builder.ExportAsClasses(typesAsTsClass, config => config
            .WithPublicProperties()
            .DontIncludeToNamespace()
        );

        var enumTypes = coreBackendAccountTypes.Where(x => x.IsEnum && x.IsPublic)
            .Union(coreWebAccountTypes.Where(x => x.IsEnum && x.IsPublic));

        builder.ExportAsEnums(enumTypes, config => config
            .DontIncludeToNamespace()
        );
    }
}