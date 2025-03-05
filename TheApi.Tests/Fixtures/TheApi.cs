namespace TheApi.Tests.Fixtures;

public class TheApiFixture(ITestOutputHelper testOutputHelper, string environment = "Development") : WebApplicationFactory<Program>
{
    public static void SetUseDevJwt(bool useDevJwt)
    {
        Environment.SetEnvironmentVariable("AppSettings__UseDevJwt", useDevJwt.ToString());
    }
    public static void UnsetUseDevJwt()
    {
        Environment.SetEnvironmentVariable("AppSettings__UseDevJwt", null);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // TODO: blow up if we're connecting to a real database and/or storage account?

        builder.UseEnvironment(environment);

        builder.ConfigureServices(services =>
        {
            // Add mock/test services to the builder

            builder.ConfigureLogging(logging =>
            {
                // https://www.meziantou.net/how-to-view-logs-from-ilogger-in-xunitdotnet.htm
                // https://blog.martincostello.com/writing-logs-to-xunit-test-output/
                services.AddSingleton<ILoggerProvider>(new XUnitLoggerProvider(testOutputHelper));
            });

            /* For example:
            var oldDb = services.FirstOrDefault(s => s.ServiceType == typeof(MyDbContext));
            if (oldDb != null)
            {
                services.Remove(oldDb);
            }

            services.AddDbContext<MyDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: "MyDb"));
            */

        });

        return base.CreateHost(builder);
    }
}
