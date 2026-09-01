using CoreBackend.Infrastructure.Configurations;

namespace CoreWeb.Tests;

[SetUpFixture]
public class RunOncePerTestRun
{
    private const string EnvironmentName = "Test";

    [OneTimeSetUp]
    public void SetUp()
    {
        if (Environment.GetEnvironmentVariable(ConsoleAppSettings.AspNetCoreEnvironmentVariable) == null)
        {
            Environment.SetEnvironmentVariable(ConsoleAppSettings.AspNetCoreEnvironmentVariable, EnvironmentName);
        }

        ConsoleAppSettings.Initialize();
    }
}
