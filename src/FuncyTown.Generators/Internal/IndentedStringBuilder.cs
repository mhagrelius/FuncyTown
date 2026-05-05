using System;
using System.Text;

namespace FuncyTown.Generators.Internal;

internal sealed class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private bool _atLineStart = true;

    public IndentedStringBuilder Append(string text)
    {
        WriteIndent();
        _sb.Append(text);
        return this;
    }

    public IndentedStringBuilder AppendLine(string text = "")
    {
        WriteIndent();
        _sb.AppendLine(text);
        _atLineStart = true;
        return this;
    }

    public IDisposable Indent()
    {
        _indent++;
        return new Releaser(this);
    }

    public IDisposable Block()
    {
        AppendLine("{");
        _indent++;
        return new Releaser(this, closeBrace: true);
    }

    public override string ToString() => _sb.ToString();

    private void WriteIndent()
    {
        if (!_atLineStart)
        {
            return;
        }

        for (var i = 0; i < _indent; i++)
        {
            _sb.Append("    ");
        }

        _atLineStart = false;
    }

    private sealed class Releaser : IDisposable
    {
        private readonly IndentedStringBuilder _owner;
        private readonly bool _closeBrace;

        public Releaser(IndentedStringBuilder owner, bool closeBrace = false)
        {
            _owner = owner;
            _closeBrace = closeBrace;
        }

        public void Dispose()
        {
            _owner._indent = Math.Max(0, _owner._indent - 1);
            if (_closeBrace)
            {
                _owner.AppendLine("}");
            }
        }
    }
}
