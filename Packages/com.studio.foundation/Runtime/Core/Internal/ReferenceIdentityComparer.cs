using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Studio.Foundation.Internal
{
    internal sealed class ReferenceIdentityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
