namespace BenefitBridge.Domain.Tests;

public class PlaceholderTests
{
    [Fact]
    public void Add_TwoPositiveIntegers_ReturnsSum()
    {
        int value1 = 1;
        int value2 = 2;
        int solution = value1 + value2;
        Assert.Equal(3, solution);
    }
}
