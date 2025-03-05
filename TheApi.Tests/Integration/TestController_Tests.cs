namespace TheApi.Tests.Integration;

[Collection("Sequential")]
public class TestController_Tests(ITestOutputHelper output) : IAsyncLifetime
{

	private JsonSerializerOptions options = new JsonSerializerOptions
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			ReferenceHandler = ReferenceHandler.IgnoreCycles
		};

	public async Task InitializeAsync()
    {
        await Task.CompletedTask;
	}
    public async Task DisposeAsync()
    {
        TheApiFixture.UnsetUseDevJwt();
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Type", "Integration")]
    public async Task Get()
    {
        // Arrange
        string email = "test-user@example.com";
        Dictionary<string, string> otherClaims = [];

        (string jwt, string error) = await TestJwtBuilder.GetTestJwt(email, otherClaims);
        jwt.ShouldNotBeNullOrWhiteSpace();
        error.ShouldBeNullOrWhiteSpace();

		TheApiFixture.SetUseDevJwt(true);
        await using TheApiFixture application = new TheApiFixture(output);
        using HttpClient client = application.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        // Act
        using HttpResponseMessage res = await client.GetAsync($"/api/test");
        string body = await res.Content.ReadAsStringAsync();

		ApiResponse? model = JsonSerializer.Deserialize<ApiResponse>(body, options);

        // Assert
        res.StatusCode.ShouldBe(HttpStatusCode.OK, jwt + ": " + body);
        model.ShouldNotBeNull(jwt + ": " + body);
        ArgumentNullException.ThrowIfNull(model);
        model.Message.ShouldBe("Success");
    }

}
