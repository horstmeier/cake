// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Cake.Core;
using Cake.Core.IO;
using Cake.Testing;
using Xunit;

namespace Cake.Core.Tests.Unit.IO
{
    public sealed class EnvironmentVariableExpanderTests
    {
        public sealed class TheExpandMethod
        {
            [Fact]
            public void Should_Expand_Windows_Style_Environment_Variables()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                environment.SetEnvironmentVariable("TEMP", "/tmp");
                var pattern = "%TEMP%/logs/**/*.log";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("/tmp/logs/**/*.log", result);
            }

            [Fact]
            public void Should_Expand_Unix_Style_Environment_Variables_Without_Braces()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                environment.SetEnvironmentVariable("HOME", "/home/user");
                var pattern = "$HOME/src/**/*.cs";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("/home/user/src/**/*.cs", result);
            }

            [Fact]
            public void Should_Expand_Unix_Style_Environment_Variables_With_Braces()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                environment.SetEnvironmentVariable("PROJECT_ROOT", "/var/projects/myapp");
                var pattern = "${PROJECT_ROOT}/build/**/*.dll";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("/var/projects/myapp/build/**/*.dll", result);
            }

            [Fact]
            public void Should_Expand_Multiple_Environment_Variables()
            {
                // Given
                var environment = FakeEnvironment.CreateWindowsEnvironment();
                environment.SetEnvironmentVariable("DRIVE", "C:");
                environment.SetEnvironmentVariable("PROJECT", "MyProject");
                var pattern = "%DRIVE%/src/%PROJECT%/**/*.cs";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("C:/src/MyProject/**/*.cs", result);
            }

            [Fact]
            public void Should_Support_Both_Windows_And_Unix_Style_In_Same_Pattern()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                environment.SetEnvironmentVariable("BASE", "/opt");
                environment.SetEnvironmentVariable("APP", "myapp");
                var pattern = "$BASE/%APP%/logs/*.log";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("/opt/myapp/logs/*.log", result);
            }

            [Fact]
            public void Should_Leave_Undefined_Variables_Unchanged()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                var pattern = "$UNDEFINED_VAR/**/*.cs";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("$UNDEFINED_VAR/**/*.cs", result);
            }

            [Fact]
            public void Should_Leave_Undefined_Windows_Variables_Unchanged()
            {
                // Given
                var environment = FakeEnvironment.CreateWindowsEnvironment();
                var pattern = "%UNDEFINED_VAR%/**/*.dll";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("%UNDEFINED_VAR%/**/*.dll", result);
            }

            [Fact]
            public void Should_Handle_Empty_Pattern()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                var pattern = "";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("", result);
            }

            [Fact]
            public void Should_Handle_Null_Pattern()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                string pattern = null;

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Null(result);
            }

            [Fact]
            public void Should_Handle_Pattern_Without_Variables()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                var pattern = "./src/**/*.cs";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("./src/**/*.cs", result);
            }

            [Fact]
            public void Should_Expand_Variables_With_Underscores()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                environment.SetEnvironmentVariable("MY_VAR_NAME", "/test/path");
                var pattern = "$MY_VAR_NAME/**/*.cs";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("/test/path/**/*.cs", result);
            }

            [Fact]
            public void Should_Expand_Variables_With_Numbers()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                environment.SetEnvironmentVariable("VAR123", "/test");
                var pattern = "${VAR123}/files/*.txt";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("/test/files/*.txt", result);
            }

            [Fact]
            public void Should_Handle_Variables_At_Different_Positions()
            {
                // Given
                var environment = FakeEnvironment.CreateUnixEnvironment();
                environment.SetEnvironmentVariable("START", "/begin");
                environment.SetEnvironmentVariable("MIDDLE", "middle");
                environment.SetEnvironmentVariable("END", "end");
                var pattern = "$START/$MIDDLE/**/$END/*.log";

                // When
                var result = EnvironmentVariableExpander.Expand(pattern, environment);

                // Then
                Assert.Equal("/begin/middle/**/end/*.log", result);
            }
        }

        public sealed class TheContainsEnvironmentVariablesMethod
        {
            [Theory]
            [InlineData("%TEMP%/logs/*.log", true)]
            [InlineData("$HOME/src/*.cs", true)]
            [InlineData("${PROJECT_ROOT}/build/*.dll", true)]
            [InlineData("./src/**/*.cs", false)]
            [InlineData("", false)]
            [InlineData(null, false)]
            [InlineData("%TEMP%/$HOME/${VAR}", true)]
            [InlineData("some/path/without/vars", false)]
            [InlineData("$", false)] // Just a dollar sign
            [InlineData("%", false)] // Just a percent sign
            public void Should_Detect_Environment_Variables(string pattern, bool expected)
            {
                // When
                var result = EnvironmentVariableExpander.ContainsEnvironmentVariables(pattern);

                // Then
                Assert.Equal(expected, result);
            }
        }
    }
}
