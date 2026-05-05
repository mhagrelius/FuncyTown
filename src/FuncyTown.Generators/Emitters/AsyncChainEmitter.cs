using FuncyTown.Generators.Internal;
using FuncyTown.Generators.Models;

namespace FuncyTown.Generators.Emitters;

internal static class AsyncChainEmitter
{
    public static void EmitAsyncMethods(IndentedStringBuilder sb, ResultAliasModel m)
    {
        if (m.IsVoidSuccess)
        {
            EmitVoidAsyncMethods(sb, m.AliasName, m.ErrorTypeFullyQualifiedName);
        }
        else
        {
            EmitValueAsyncMethods(sb, m.AliasName, m.ValueTypeFullyQualifiedName, m.ErrorTypeFullyQualifiedName);
        }
    }

    private static void EmitValueAsyncMethods(
        IndentedStringBuilder sb,
        string alias,
        string valueType,
        string errorType)
    {
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> MapAsync<TNew>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<TNew>> selector) => _inner.MapAsync(selector);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> TransformAsync<TNew>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<TNew>> selector) => MapAsync(selector);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> SelectAsync<TNew>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<TNew>> selector) => MapAsync(selector);");

        sb.AppendLine($"public async global::System.Threading.Tasks.Task<{alias}> BindAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{alias}>> next)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(next);");
            sb.AppendLine("if (_inner.IsSuccess)");
            using (sb.Block())
            {
                sb.AppendLine("return await next(_inner.Value).ConfigureAwait(false);");
            }

            sb.AppendLine("if (_inner.IsFailure)");
            using (sb.Block())
            {
                sb.AppendLine("return Failure(_inner.Error);");
            }

            sb.AppendLine("throw new global::FuncyTown.ResultException(\"Cannot BindAsync an uninitialized Result.\");");
        }

        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> ThenAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{alias}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> AndThenAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{alias}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> SelectManyAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<{alias}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> BindAsync<TNew>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>>> next) => _inner.BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> ThenAsync<TNew>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>>> next) => _inner.BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> AndThenAsync<TNew>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>>> next) => _inner.BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> SelectManyAsync<TNew>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>>> next) => _inner.BindAsync(next);");

        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<{valueType}, TNewError>> MapErrorAsync<TNewError>(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<TNewError>> selector) => _inner.MapErrorAsync(selector);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<{valueType}, TNewError>> TransformErrorAsync<TNewError>(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<TNewError>> selector) => MapErrorAsync(selector);");
        EmitWrappedInnerAsyncCall(sb, alias, "MapErrorAsync", $"global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{errorType}>> selector", "_inner.MapErrorAsync(selector)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> TransformErrorAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{errorType}>> selector) => MapErrorAsync(selector);");

        EmitWrappedInnerAsyncCall(sb, alias, "RecoverAsync", $"global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{valueType}>> fallback", "_inner.RecoverAsync(fallback)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> OrElseAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{valueType}>> fallback) => RecoverAsync(fallback);");
        EmitRecoverWithAsync(sb, alias, "global::System.Func<" + errorType + ", global::System.Threading.Tasks.Task<" + alias + ">> fallback", "fallback(_inner.Error)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> OrElseThenAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{alias}>> fallback) => RecoverWithAsync(fallback);");
        EmitWrappedInnerAsyncCall(sb, alias, "EnsureAsync", $"global::System.Func<{valueType}, global::System.Threading.Tasks.Task<bool>> predicate, {errorType} error", "_inner.EnsureAsync(predicate, error)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> ValidateAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<bool>> predicate, {errorType} error) => EnsureAsync(predicate, error);");

        EmitWrappedInnerAsyncCall(sb, alias, "OnSuccessAsync", $"global::System.Func<{valueType}, global::System.Threading.Tasks.Task> action", "_inner.OnSuccessAsync(action)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> IfSuccessfulAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task> action) => OnSuccessAsync(action);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> TapAsync(global::System.Func<{valueType}, global::System.Threading.Tasks.Task> action) => OnSuccessAsync(action);");
        EmitWrappedInnerAsyncCall(sb, alias, "OnFailureAsync", $"global::System.Func<{errorType}, global::System.Threading.Tasks.Task> action", "_inner.OnFailureAsync(action)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> IfFailedAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task> action) => OnFailureAsync(action);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> TapErrorAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task> action) => OnFailureAsync(action);");

        sb.AppendLine($"public global::System.Threading.Tasks.Task<TResult> MatchAsync<TResult>(global::System.Func<{valueType}, global::System.Threading.Tasks.Task<TResult>> onSuccess, global::System.Func<{errorType}, global::System.Threading.Tasks.Task<TResult>> onFailure) => _inner.MatchAsync(onSuccess, onFailure);");
    }

    private static void EmitVoidAsyncMethods(
        IndentedStringBuilder sb,
        string alias,
        string errorType)
    {
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> MapAsync<TNew>(global::System.Func<global::System.Threading.Tasks.Task<TNew>> selector)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(selector);");
            sb.AppendLine("return _inner.MapAsync(_ => selector());");
        }

        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> TransformAsync<TNew>(global::System.Func<global::System.Threading.Tasks.Task<TNew>> selector) => MapAsync(selector);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> SelectAsync<TNew>(global::System.Func<global::System.Threading.Tasks.Task<TNew>> selector) => MapAsync(selector);");

        sb.AppendLine($"public async global::System.Threading.Tasks.Task<{alias}> BindAsync(global::System.Func<global::System.Threading.Tasks.Task<{alias}>> next)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(next);");
            sb.AppendLine("if (_inner.IsSuccess)");
            using (sb.Block())
            {
                sb.AppendLine("return await next().ConfigureAwait(false);");
            }

            sb.AppendLine("if (_inner.IsFailure)");
            using (sb.Block())
            {
                sb.AppendLine("return Failure(_inner.Error);");
            }

            sb.AppendLine("throw new global::FuncyTown.ResultException(\"Cannot BindAsync an uninitialized Result.\");");
        }

        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> ThenAsync(global::System.Func<global::System.Threading.Tasks.Task<{alias}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> AndThenAsync(global::System.Func<global::System.Threading.Tasks.Task<{alias}>> next) => BindAsync(next);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> SelectManyAsync(global::System.Func<global::System.Threading.Tasks.Task<{alias}>> next) => BindAsync(next);");
        EmitVoidGenericBindAsync(sb, "BindAsync", errorType);
        EmitVoidGenericBindAsync(sb, "ThenAsync", errorType);
        EmitVoidGenericBindAsync(sb, "AndThenAsync", errorType);
        EmitVoidGenericBindAsync(sb, "SelectManyAsync", errorType);

        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<global::FuncyTown.Unit, TNewError>> MapErrorAsync<TNewError>(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<TNewError>> selector) => _inner.MapErrorAsync(selector);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<global::FuncyTown.Unit, TNewError>> TransformErrorAsync<TNewError>(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<TNewError>> selector) => MapErrorAsync(selector);");
        EmitWrappedInnerAsyncCall(sb, alias, "MapErrorAsync", $"global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{errorType}>> selector", "_inner.MapErrorAsync(selector)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> TransformErrorAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{errorType}>> selector) => MapErrorAsync(selector);");

        EmitRecoverWithAsync(sb, alias, "global::System.Func<" + errorType + ", global::System.Threading.Tasks.Task<" + alias + ">> fallback", "fallback(_inner.Error)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> OrElseThenAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task<{alias}>> fallback) => RecoverWithAsync(fallback);");

        EmitVoidOnSuccessAsync(sb, alias);
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> IfSuccessfulAsync(global::System.Func<global::System.Threading.Tasks.Task> action) => OnSuccessAsync(action);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> TapAsync(global::System.Func<global::System.Threading.Tasks.Task> action) => OnSuccessAsync(action);");
        EmitWrappedInnerAsyncCall(sb, alias, "OnFailureAsync", $"global::System.Func<{errorType}, global::System.Threading.Tasks.Task> action", "_inner.OnFailureAsync(action)");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> IfFailedAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task> action) => OnFailureAsync(action);");
        sb.AppendLine($"public global::System.Threading.Tasks.Task<{alias}> TapErrorAsync(global::System.Func<{errorType}, global::System.Threading.Tasks.Task> action) => OnFailureAsync(action);");

        sb.AppendLine($"public global::System.Threading.Tasks.Task<TResult> MatchAsync<TResult>(global::System.Func<global::System.Threading.Tasks.Task<TResult>> onSuccess, global::System.Func<{errorType}, global::System.Threading.Tasks.Task<TResult>> onFailure)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(onSuccess);");
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(onFailure);");
            sb.AppendLine("return _inner.MatchAsync(_ => onSuccess(), onFailure);");
        }
    }

    private static void EmitWrappedInnerAsyncCall(
        IndentedStringBuilder sb,
        string alias,
        string methodName,
        string parameters,
        string innerCall)
    {
        sb.AppendLine($"public async global::System.Threading.Tasks.Task<{alias}> {methodName}({parameters})");
        using (sb.Block())
        {
            sb.AppendLine($"return new(await {innerCall}.ConfigureAwait(false));");
        }
    }

    private static void EmitRecoverWithAsync(
        IndentedStringBuilder sb,
        string alias,
        string parameters,
        string failureExpression)
    {
        sb.AppendLine($"public async global::System.Threading.Tasks.Task<{alias}> RecoverWithAsync({parameters})");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(fallback);");
            sb.AppendLine("if (_inner.IsSuccess)");
            using (sb.Block())
            {
                sb.AppendLine("return this;");
            }

            sb.AppendLine("if (_inner.IsFailure)");
            using (sb.Block())
            {
                sb.AppendLine($"return await {failureExpression}.ConfigureAwait(false);");
            }

            sb.AppendLine("throw new global::FuncyTown.ResultException(\"Cannot RecoverWithAsync on an uninitialized Result.\");");
        }
    }

    private static void EmitVoidGenericBindAsync(IndentedStringBuilder sb, string methodName, string errorType)
    {
        sb.AppendLine($"public global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>> {methodName}<TNew>(global::System.Func<global::System.Threading.Tasks.Task<global::FuncyTown.Result<TNew, {errorType}>>> next)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(next);");
            sb.AppendLine("return _inner.BindAsync(_ => next());");
        }
    }

    private static void EmitVoidOnSuccessAsync(IndentedStringBuilder sb, string alias)
    {
        sb.AppendLine($"public async global::System.Threading.Tasks.Task<{alias}> OnSuccessAsync(global::System.Func<global::System.Threading.Tasks.Task> action)");
        using (sb.Block())
        {
            sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(action);");
            sb.AppendLine("return new(await _inner.OnSuccessAsync(_ => action()).ConfigureAwait(false));");
        }
    }
}
