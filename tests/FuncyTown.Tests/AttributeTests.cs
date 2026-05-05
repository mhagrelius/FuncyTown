using Shouldly;
using Xunit;

namespace FuncyTown.Tests;

[ErrorUnion]
internal partial class AttributeFakeErrorUnion;

[ErrorCase]
internal sealed partial class AttributeFakeErrorCase : AttributeFakeErrorUnion;

public partial class AttributeTests
{
    private sealed record FakeError(string Code, string Message) : IError;

    [Result<int, FakeError>]
    private readonly partial record struct DummyResult;

    [Fact]
    public void Attribute_can_be_applied_to_partial_record_struct()
    {
        // The partial record struct above must compile with the attribute.
        // Generator emits no members in Phase 1's earliest tasks; this only
        // verifies the attribute itself is well-formed and reachable.
        typeof(DummyResult).Name.ShouldBe(nameof(DummyResult));
    }

    [Fact]
    public void Attribute_default_values()
    {
        _ = new FakeError("X", "x");
        var attr = new ResultAttribute<int, FakeError>();
        attr.Implicit.ShouldBeTrue();
        attr.Kind.ShouldBe(ResultKind.Struct);
    }

    [Fact]
    public void Single_arg_attribute_default_values()
    {
        var attr = new ResultAttribute<FakeError>();
        attr.Implicit.ShouldBeTrue();
        attr.Kind.ShouldBe(ResultKind.Struct);
    }

    [Fact]
    public void Error_union_attributes_can_be_applied_to_classes()
    {
        _ = AttributeFakeErrorUnion.Combine([]);
        _ = new AttributeFakeErrorCase();

        typeof(AttributeFakeErrorUnion)
            .GetCustomAttributes(typeof(ErrorUnionAttribute), inherit: false)
            .Length
            .ShouldBe(1);

        typeof(AttributeFakeErrorCase)
            .GetCustomAttributes(typeof(ErrorCaseAttribute), inherit: false)
            .Length
            .ShouldBe(1);
    }

    [Fact]
    public void Error_union_attributes_have_parameterless_constructors()
    {
        new ErrorUnionAttribute().ShouldNotBeNull();
        new ErrorCaseAttribute().ShouldNotBeNull();
    }
}
