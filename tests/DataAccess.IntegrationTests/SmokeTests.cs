using Shouldly;
using Xunit;

namespace DataAccess.IntegrationTests;

public class SmokeTests
{
    [Fact]
    public void Integration_project_builds()
    {
        true.ShouldBeTrue();
    }
}
