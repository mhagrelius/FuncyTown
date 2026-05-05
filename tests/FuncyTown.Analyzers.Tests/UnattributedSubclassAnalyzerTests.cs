using FuncyTown.Analyzers.Tests.Verifiers;
using Xunit;

namespace FuncyTown.Analyzers.Tests;

public sealed class UnattributedSubclassAnalyzerTests
{
    [Fact]
    public Task Reports_on_direct_subclass_of_error_union_without_error_case_attribute()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            public sealed partial class {|FT0005:NotFound|} : UserError;
            """;

        return CSharpAnalyzerVerifier<UnattributedSubclassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_transitive_subclass_of_error_union_without_error_case_attribute()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public partial class NotFound : UserError;

            public sealed partial class {|FT0005:SpecificNotFound|} : NotFound;
            """;

        return CSharpAnalyzerVerifier<UnattributedSubclassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Reports_on_error_union_subclass_without_error_case_attribute()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorUnion]
            public partial class {|FT0005:NestedUserError|} : UserError;
            """;

        return CSharpAnalyzerVerifier<UnattributedSubclassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_attributed_error_case_subclass()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;

            [ErrorCase]
            public sealed partial class NotFound : UserError;
            """;

        return CSharpAnalyzerVerifier<UnattributedSubclassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_unrelated_inheritance()
    {
        const string source = """
            public class BaseError;

            public sealed class DerivedError : BaseError;
            """;

        return CSharpAnalyzerVerifier<UnattributedSubclassAnalyzer>.VerifyAnalyzerAsync(source);
    }

    [Fact]
    public Task Does_not_report_generated_synthetic_many_subclass()
    {
        const string source = """
            using FuncyTown;

            [ErrorUnion]
            public partial class UserError;
            """;

        return CSharpAnalyzerVerifier<UnattributedSubclassAnalyzer>.VerifyAnalyzerAsync(source);
    }
}
