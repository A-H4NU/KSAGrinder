using System;
using System.Collections.Generic;
using System.Linq;

namespace KSAGrinder.Extensions
{
    public static class CloneExtension
    {
        public static IEnumerable<T> Clone<T>(this IEnumerable<T> list) where T : ICloneable => list.Select(item => (T)item.Clone());
    }
}
