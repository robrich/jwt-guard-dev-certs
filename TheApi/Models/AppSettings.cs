namespace TheApi.Models;

public class AppSettings
{
    public string EnvironmentName { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string AuthorityDomain { get; set; } = "";
    public string SignInPolicyId { get; set; } = "";
    public bool UseDevJwt { get; set; }
}
