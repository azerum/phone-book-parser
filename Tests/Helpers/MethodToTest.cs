using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Library;

namespace Tests.Helpers
{
    /// <summary>
    /// <para>
    /// Wraps a lambda with <see cref="IPhoneBook"/> method class and overrides
    /// ToString() so that is returns that methods name
    /// </para>
    /// <para>
    /// Convinient to use with NUNit's [ValueSource] attribute, as test output
    /// will contain readable names of the methods, not cryptic names of lambdas
    /// </para>
    /// </summary>
    public class MethodToTest
    {
        public string Name { get; }
        public Func<IPhoneBook, IAsyncEnumerable<object>> Call { get; }

        public MethodToTest(
            Expression<Func<IPhoneBook, IAsyncEnumerable<object>>> method
        )
        {
            if (method.Body.NodeType != ExpressionType.Call)
            {
                throw new ArgumentException(
                    "'method' must be a lambda with single IPhoneBook method call"
                );
            }

            var call = (MethodCallExpression)method.Body;
            Name = call.Method.Name;

            Call = method.Compile();
        }

        public override string ToString() => Name;
    }
}
