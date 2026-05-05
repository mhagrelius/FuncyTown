using FuncyTown.Generators.Internal;
using FuncyTown.Generators.Models;

namespace FuncyTown.Generators.Emitters;

internal static class CrossAliasOverloadEmitter
{
    public static bool CanEmit(ResultAliasModel currentAlias, ResultAliasModel siblingAlias)
    {
        if (siblingAlias.AliasFullyQualifiedName == currentAlias.AliasFullyQualifiedName
            || !AliasTaskExtensionEmitter.CanEmit(siblingAlias))
        {
            return false;
        }

        return !AliasTaskExtensionEmitter.IsPubliclyVisible(currentAlias)
            || AliasTaskExtensionEmitter.IsPubliclyVisible(siblingAlias);
    }

    public static void Emit(IndentedStringBuilder sb, ResultAliasModel currentAlias, ResultAliasModel siblingAlias)
    {
        if (currentAlias.IsVoidSuccess)
        {
            EmitVoidSuccessOverloads(sb, siblingAlias);
        }
        else
        {
            EmitValueSuccessOverloads(sb, currentAlias.ValueTypeFullyQualifiedName, siblingAlias);
        }
    }

    private static void EmitValueSuccessOverloads(
        IndentedStringBuilder sb,
        string valueType,
        ResultAliasModel siblingAlias)
    {
        var siblingType = siblingAlias.AliasFullyQualifiedName;

        EmitValueSyncBind(sb, "Bind", valueType, siblingType);
        sb.AppendLine($"public {siblingType} Then(global::System.Func<{valueType}, {siblingType}> next) => Bind(next);");
        sb.AppendLine($"public {siblingType} AndThen(global::System.Func<{valueType}, {siblingType}> next) => Bind(next);");
        sb.AppendLine($"public {siblingType} SelectMany(global::System.Func<{valueType}, {siblingType}> next) => Bind(next);");
        EmitValueAsyncBind(sb, "BindAsync", valueType, siblingType);
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{siblingType}> ThenAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{siblingType}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{siblingType}> AndThenAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{siblingType}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{siblingType}> SelectManyAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{siblingType}>> next) => BindAsync(next);");
    }

    private static void EmitVoidSuccessOverloads(IndentedStringBuilder sb, ResultAliasModel siblingAlias)
    {
        var siblingType = siblingAlias.AliasFullyQualifiedName;

        EmitVoidSyncBind(sb, "Bind", siblingType);
        sb.AppendLine($"public {siblingType} Then(global::System.Func<{siblingType}> next) => Bind(next);");
        sb.AppendLine($"public {siblingType} AndThen(global::System.Func<{siblingType}> next) => Bind(next);");
        EmitVoidAsyncBind(sb, "BindAsync", siblingType);
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{siblingType}> ThenAsync(global::System.Func<global::System.Threading.Tasks.Task<{siblingType}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{siblingType}> AndThenAsync(global::System.Func<global::System.Threading.Tasks.Task<{siblingType}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{siblingType}> SelectManyAsync(global::System.Func<global::System.Threading.Tasks.Task<{siblingType}>> next) => BindAsync(next);");
    }

    private static void EmitValueSyncBind(
        IndentedStringBuilder sb,
        string methodName,
        string valueType,
        string siblingType)
    {
        sb.AppendLine($"public {siblingType} {methodName}(global::System.Func<{valueType}, {siblingType}> next)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(next);");
            sb.AppendLine("if (_inner.IsSuccess)");
            using (sb.Block())
            {
                sb.AppendLine("return next(_inner.Value);");
            }

            EmitSiblingFailureOrUninitialized(sb, siblingType, methodName);
        }
    }

    private static void EmitVoidSyncBind(
        IndentedStringBuilder sb,
        string methodName,
        string siblingType)
    {
        sb.AppendLine($"public {siblingType} {methodName}(global::System.Func<{siblingType}> next)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(next);");
            sb.AppendLine("if (_inner.IsSuccess)");
            using (sb.Block())
            {
                sb.AppendLine("return next();");
            }

            EmitSiblingFailureOrUninitialized(sb, siblingType, methodName);
        }
    }

    private static void EmitValueAsyncBind(
        IndentedStringBuilder sb,
        string methodName,
        string valueType,
        string siblingType)
    {
        sb.AppendLine($"public async global::System.Threading.Tasks.Task<{siblingType}> {methodName}(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{siblingType}>> next)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(next);");
            sb.AppendLine("if (_inner.IsSuccess)");
            using (sb.Block())
            {
                sb.AppendLine("return await next(_inner.Value).ConfigureAwait(false);");
            }

            EmitSiblingFailureOrUninitialized(sb, siblingType, methodName);
        }
    }

    private static void EmitVoidAsyncBind(
        IndentedStringBuilder sb,
        string methodName,
        string siblingType)
    {
        sb.AppendLine($"public async global::System.Threading.Tasks.Task<{siblingType}> {methodName}(global::System.Func<global::System.Threading.Tasks.Task<{siblingType}>> next)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(next);");
            sb.AppendLine("if (_inner.IsSuccess)");
            using (sb.Block())
            {
                sb.AppendLine("return await next().ConfigureAwait(false);");
            }

            EmitSiblingFailureOrUninitialized(sb, siblingType, methodName);
        }
    }

    private static void EmitSiblingFailureOrUninitialized(
        IndentedStringBuilder sb,
        string siblingType,
        string methodName)
    {
        sb.AppendLine("if (_inner.IsFailure)");
        using (sb.Block())
        {
            sb.AppendLine($"return {siblingType}.Failure(_inner.Error);");
        }

        sb.AppendLine($"throw new global::FuncyTown.ResultException(\"Cannot {methodName} an uninitialized Result.\");");
    }
}
