using FuncyTown.Analyzers.CodeFixes;
using FuncyTown.Analyzers.Tests.Verifiers;
using Xunit;

namespace FuncyTown.Analyzers.Tests;

public sealed class UnsealedErrorCaseAnalyzerTests
{
    [Fact]
    public Task Reports_on_unsealed_class_error_case()
    {
        const string source = """
            using FuncyTown;

            [ErrorCase]
            public partial class {|FT0006:NotFound|};
            """;

        return CSharpAnalyzerVerifier<UnsealedErrorCaseAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_unsealed_record_class_error_case()
    {
        const string source = """
            using FuncyTown;

            [ErrorCase]
            public partial record class {|FT0006:Invalid|};
            """;

        return CSharpAnalyzerVerifier<UnsealedErrorCaseAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_already_sealed_error_case()
    {
        const string source = """
            using FuncyTown;

            [ErrorCase]
            public sealed partial class NotFound;
            """;

        return CSharpAnalyzerVerifier<UnsealedErrorCaseAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_non_error_case_class()
    {
        const string source = """
            public partial class NotFound;
            """;

        return CSharpAnalyzerVerifier<UnsealedErrorCaseAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Code_fix_adds_sealed_to_class_error_case()
    {
        const string source = """
            using FuncyTown;

            [ErrorCase]
            public partial class {|FT0006:NotFound|};
            """;

        const string fixedSource = """
            using FuncyTown;

            [ErrorCase]
            public sealed partial class NotFound;
            """;

        return CSharpCodeFixVerifier<UnsealedErrorCaseAnalyzer, UnsealedErrorCaseCodeFix>.VerifyCodeFixAsync(
            source,
            fixedSource);
    }

    [Fact]
    public Task Code_fix_preserves_accessibility_before_partial_record_class()
    {
        const string source = """
            using FuncyTown;

            [ErrorCase]
            internal partial record class {|FT0006:Invalid|};
            """;

        const string fixedSource = """
            using FuncyTown;

            [ErrorCase]
            internal sealed partial record class Invalid;
            """;

        return CSharpCodeFixVerifier<UnsealedErrorCaseAnalyzer, UnsealedErrorCaseCodeFix>.VerifyCodeFixAsync(
            source,
            fixedSource);
    }

    [Fact]
    public Task Code_fix_replaces_abstract_with_sealed()
    {
        const string source = """
            using FuncyTown;

            [ErrorCase]
            public abstract partial class {|FT0006:NotFound|};
            """;

        const string fixedSource = """
            using FuncyTown;

            [ErrorCase]
            public sealed partial class NotFound;
            """;

        return CSharpCodeFixVerifier<UnsealedErrorCaseAnalyzer, UnsealedErrorCaseCodeFix>.VerifyCodeFixAsync(
            source,
            fixedSource);
    }

    [Fact]
    public Task Code_fix_does_not_swap_abstract_for_sealed_when_descendants_exist()
    {
        // Replacing `abstract` with `sealed` here would silently break `Derived`.
        // The codefix declines to offer itself; the diagnostic still fires so the
        // user is on the hook to remove `abstract` or move the `[ErrorCase]` marker.
        const string source = """
            using FuncyTown;

            [ErrorCase]
            public abstract partial class {|FT0006:Base|};

            [ErrorCase]
            public sealed partial class Derived : Base;
            """;

        return CSharpCodeFixVerifier<UnsealedErrorCaseAnalyzer, UnsealedErrorCaseCodeFix>.VerifyCodeFixAsync(
            source,
            source);
    }
}
