// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;

namespace Cake.Core.IO
{
    /// <summary>
    /// Expands environment variables in strings.
    /// </summary>
    internal static class EnvironmentVariableExpander
    {
        // Matches %VAR% (Windows style)
        private static readonly Regex WindowsPattern = new Regex(
            @"%(?<name>[A-Za-z_][A-Za-z0-9_]*)%",
            RegexOptions.Compiled);

        // Matches $VAR or ${VAR} (Unix style)
        private static readonly Regex UnixPattern = new Regex(
            @"\$(?:\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}|(?<name>[A-Za-z_][A-Za-z0-9_]*))",
            RegexOptions.Compiled);

        /// <summary>
        /// Expands environment variables in the specified pattern.
        /// Supports both Windows (%VAR%) and Unix ($VAR, ${VAR}) syntax.
        /// </summary>
        /// <param name="pattern">The pattern containing environment variables.</param>
        /// <param name="environment">The environment.</param>
        /// <returns>The pattern with expanded environment variables.</returns>
        public static string Expand(string pattern, ICakeEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return pattern;
            }

            ArgumentNullException.ThrowIfNull(environment);

            // Expand Windows-style variables (%VAR%)
            var result = WindowsPattern.Replace(pattern, match =>
            {
                var varName = match.Groups["name"].Value;
                var value = environment.GetEnvironmentVariable(varName);
                return value ?? match.Value; // Keep original if not found
            });

            // Expand Unix-style variables ($VAR and ${VAR})
            result = UnixPattern.Replace(result, match =>
            {
                var varName = match.Groups["name"].Value;
                var value = environment.GetEnvironmentVariable(varName);
                return value ?? match.Value; // Keep original if not found
            });

            return result;
        }

        /// <summary>
        /// Checks if the pattern contains environment variables.
        /// </summary>
        /// <param name="pattern">The pattern to check.</param>
        /// <returns>True if the pattern contains environment variables; otherwise false.</returns>
        public static bool ContainsEnvironmentVariables(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            return WindowsPattern.IsMatch(pattern) || UnixPattern.IsMatch(pattern);
        }
    }
}
