using Shouldly;
using Xunit;

namespace DataAccess.UnitTests;

public class SmokeTests
{
    [Fact]
    public void Solution_bootstrap_is_green()
    {
        var answer = 40 + 2;
        answer.ShouldBe(42);
    }
}
