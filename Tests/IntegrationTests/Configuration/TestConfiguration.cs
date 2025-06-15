namespace IntegrationTests.Configuration;

public class TestConfiguration
{
    public string VehicleServiceUrl { get; set; } = string.Empty;
    public string InsuranceServiceUrl { get; set; } = string.Empty;
    public string TestUserId { get; set; } = string.Empty;
    public string TestPolicyId { get; set; } = string.Empty;
    public List<string> TestCoverageIds { get; set; } = new();
}
