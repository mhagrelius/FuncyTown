using Shouldly;
using Xunit;

namespace FuncyTown.Tests;

public class ResultExceptionTests
{
    [Fact]
    public void Default_message_is_descriptive()
    {
        var ex = new ResultException("Cannot access Value on a failed Result.");
        ex.Message.ShouldBe("Cannot access Value on a failed Result.");
    }

    [Fact]
    public void Inherits_from_invalid_operation()
    {
        new ResultException("test").ShouldBeAssignableTo<InvalidOperationException>();
    }
}
