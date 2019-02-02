using System;
using System.Collections.Generic;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    internal static class StringAndTextExtensions
    {
        internal static string ToLowerCase(this bool value) => value ? "true" : "false"; // to avoid .ToString().ToLower() allocation
    }
}
