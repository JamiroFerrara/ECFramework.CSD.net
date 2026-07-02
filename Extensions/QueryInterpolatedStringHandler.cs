using System;
using System.Collections.Generic;
using System.Text;

namespace ECFramework
{
    /// <summary>
    /// Contract for a parameterized query produced by <see cref="QueryInterpolatedStringHandler"/>.
    /// The handler rewrites interpolated expressions into @pN placeholders and collects
    /// the corresponding values into a dictionary, enabling Dapper to send parameterized
    /// SQL to the database.
    /// </summary>
    public interface IQueryText
    {
        /// <summary>The sanitized SQL string with @pN parameter placeholders.</summary>
        string CommandText { get; }

        /// <summary>The parameter values keyed by placeholder name (p0, p1, …).</summary>
        IReadOnlyDictionary<string, object?> Parameters { get; }
    }

    /// <summary>
    /// Internal builder that accumulates the parameterized SQL and its parameter values.
    /// This class is not exposed outside the extension layer — the compiler generates
    /// <see cref="QueryInterpolatedStringHandler"/> temporaries that delegate here.
    /// </summary>
    internal sealed class QueryTextBuilder
    {
        private readonly StringBuilder _sb;
        private readonly Dictionary<string, object?> _parameters;
        private int _parameterIndex;

        public QueryTextBuilder(int literalLength)
        {
            // Pre-allocate: literal text + generous buffer for @pN placeholders
            _sb = new StringBuilder(literalLength + (literalLength >> 1) + 64);
            _parameters = new Dictionary<string, object?>();
            _parameterIndex = 0;
        }

        public string CommandText => _sb.ToString();
        public IReadOnlyDictionary<string, object?> Parameters => _parameters;

        public void AppendLiteral(string s) => _sb.Append(s);

        /// <summary>
        /// `:p` format specifier → param (@pN); bare → inline as literal identifier.
        /// </summary>
        public void AppendFormatted<T>(T value, string? format = null)
        {
            if (format == "p")
            {
                var name = $"p{_parameterIndex}";
                _parameters.Add(name, value!);
                _sb.Append('@');
                _sb.Append(name);
                _parameterIndex++;
            }
            else
            {
                _sb.Append(value?.ToString() ?? string.Empty);
            }
        }
    }
}

// ── Polyfill for netcoreapp2.1 (InterpolatedStringHandlerAttribute is .NET 6+) ──
// The C# 10+ compiler recognizes any type with this fully-qualified name in scope,
// regardless of which framework it's defined in. Providing it here makes the attribute
// available on all target frameworks. The struct below carries the attribute so
// the compiler routes interpolated-string literals through it.
namespace System.Runtime.CompilerServices
{
    /// <summary>Indicates that the attributed type handles interpolated strings.</summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerAttribute : Attribute { }

    /// <summary>
    /// Applied to an <see cref="InterpolatedStringHandlerAttribute"/>-annotated type's
    /// AppendFormatted method to indicate which argument positions supply format values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
    {
        public InterpolatedStringHandlerArgumentAttribute(string argument) { }
    }
}

namespace ECFramework
{
    /// <summary>
    /// A custom-tailored <see cref="System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute"/>
    /// that intercepts <c>$"..."</c> expressions at compile time.
    ///
    /// When the compiler sees a call like <c>context.Query($"SELECT * FROM users WHERE id = {id:p}")</c>,
    /// it checks whether the first parameter of any accessible method is a handler type.
    /// Since this struct carries <see cref="InterpolatedStringHandlerAttribute"/> and appears
    /// as the first parameter of the <see cref="DapperDbContextExtensions"/> overloads,
    /// the compiler routes <c>$"..."</c> through this struct: calling <see cref="AppendLiteral"/>
    /// for static text and <see cref="AppendFormatted{T}"/> for each interpolated expression.
    /// 
    /// <para>
    /// <strong>Default (bare <c>{expr}</c>) inlines as a literal identifier</strong> (table names, schemas, column names in ORDER BY).
    /// Use <c>{expr:p}</c> to generate a parameterized <c>@pN</c> placeholder (WHERE, VALUES, SET).
    /// </para>
    /// </summary>
    [System.Runtime.CompilerServices.InterpolatedStringHandler]
    public struct QueryInterpolatedStringHandler
    {
        private readonly QueryTextBuilder _builder;

        /// <summary>
        /// Called by the compiler. <paramref name="literalLength"/> is the total character count
        /// of the static text portions; <paramref name="formattedCount"/> is the number of
        /// interpolated expressions.
        /// </summary>
        public QueryInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            _builder = new QueryTextBuilder(literalLength);
        }

        /// <summary>Called by the compiler for each literal segment of the interpolated string.</summary>
        public void AppendLiteral(string s) => _builder.AppendLiteral(s);

        /// <summary>Called by the compiler for each interpolated expression.</summary>
        public void AppendFormatted<T>(T t, string? format = null) => _builder.AppendFormatted(t, format);

        /// <summary>The sanitized SQL string with @pN parameter placeholders.</summary>
        public string CommandText => _builder.CommandText;

        /// <summary>The parameter values keyed by placeholder name (p0, p1, …).</summary>
        public IReadOnlyDictionary<string, object?> Parameters => _builder.Parameters;
    }
}