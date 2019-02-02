using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class ReflectionExtensions
    {
        internal static bool IsLinqPad(this Assembly assembly) => assembly.FullName.ToUpper().Contains("LINQPAD");
    }
}
