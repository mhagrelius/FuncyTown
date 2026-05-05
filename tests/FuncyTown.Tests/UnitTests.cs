using Shouldly;
using Xunit;

namespace FuncyTown.Tests;

public class UnitTests
{
    [Fact]
    public void Default_unit_equals_default_unit()
    {
        Unit.Value.ShouldBe(default(Unit));
    }

    [Fact]
    public void All_units_are_equal()
    {
        var a = new Unit();
        var b = Unit.Value;
        a.Equals(b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_is_zero()
    {
        Unit.Value.GetHashCode().ShouldBe(0);
    }

    [Fact]
    public void ToString_is_paren_paren()
    {
        Unit.Value.ToString().ShouldBe("()");
    }
}
