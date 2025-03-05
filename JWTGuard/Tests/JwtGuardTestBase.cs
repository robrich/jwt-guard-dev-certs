using JWTGuard.Helpers;

using Microsoft.AspNetCore.Mvc.Testing;
using TheApi.Tests.Fixtures;
using Xunit;
using static Duende.IdentityServer.Models.IdentityResources;

namespace JWTGuard.Tests;

/// <summary>
/// Base class for JWT Guard test cases.
/// </summary>
/// <param name="factory"></param>
[Collection(JwtGuardTestCollection.CollectionName)]
public abstract class JwtGuardTestBase(TargetApiWebApplicationFactory factory) : IAsyncLifetime
{
    private AsyncServiceScope _serviceScope;

    /// <summary>
    /// The <see cref="TargetApiWebApplicationFactory"/>
    /// </summary>
    protected TargetApiWebApplicationFactory Factory { get; } = factory;
    
    /// <summary>
    /// A <see cref="HttpClient"/> that can access the target Web API.
    /// </summary>
    protected HttpClient? Client { get; private set; }

    /// <summary>
    /// A <see cref="IServiceProvider"/> to request services from.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    protected IServiceProvider? ServiceProvider { get; private set; }
    
    /// <summary>
    /// Initializes the base class for a test run.
    /// </summary>
    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("AppSettings__UseDevJwt", "true");

        // Have to get a JWT to turn on dev certs for this project
        (string jwt, string error) = await TestJwtBuilder.GetTestJwt("jon@doe.com", []);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(jwt, nameof(jwt));
        Assert.Equal("", error);

        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost/")
        });

        _serviceScope = Factory.Services.CreateAsyncScope();
        ServiceProvider = _serviceScope.ServiceProvider;
        
    }

    /// <summary>
    /// Disposes the service scope and every service requested during the test run.
    /// </summary>
    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("AppSettings__UseDevJwt", null);

        await _serviceScope.DisposeAsync();
    }
}
