// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Cake.Core.IO;
using Cake.Core.Tests.Fixtures;
using Cake.Testing;
using NSubstitute;
using Xunit;

namespace Cake.Core.Tests.Unit.IO.Globbing
{
    public sealed class GlobberTests
    {
        public sealed class TheConstructor
        {
            [Fact]
            public void Should_Throw_If_File_System_Is_Null()
            {
                // Given, When
                var environment = Substitute.For<ICakeEnvironment>();
                var result = Record.Exception(() => new Globber(null, environment));

                // Then
                AssertEx.IsArgumentNullException(result, "fileSystem");
            }

            [Fact]
            public void Should_Throw_If_Environment_Is_Null()
            {
                // Given
                var fileSystem = Substitute.For<IFileSystem>();

                // When
                var result = Record.Exception(() => new Globber(fileSystem, null));

                // Then
                AssertEx.IsArgumentNullException(result, "environment");
            }
        }

        public sealed class TheMatchMethod
        {
            public sealed class WithDirectoryPredicate
            {
                [Fact]
                public void Should_Return_Paths_Not_Affected_By_Walker_Hints()
                {
                    // Given
                    var fixture = GlobberFixture.UnixLike();
                    var predicate = new Func<IFileSystemInfo, bool>(i =>
                        i.Path.FullPath != "/Working/Bar");

                    // When
                    var result = fixture.Match("./**/Qux.h", predicate);

                    // Then
                    Assert.Single(result);
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.h");
                }

                [Fact]
                public void Should_Not_Return_Path_If_Walker_Hint_Matches_Part_Of_Pattern()
                {
                    // Given
                    var fixture = GlobberFixture.UnixLike();
                    var predicate = new Func<IFileSystemInfo, bool>(i =>
                        i.Path.FullPath != "/Working/Bar");

                    // When
                    var result = fixture.Match("/Working/Bar/Qux.h", predicate);

                    // Then
                    Assert.Empty(result);
                }

                [Fact]
                public void Should_Not_Return_Path_If_Walker_Hint_Exactly_Match_Pattern()
                {
                    // Given
                    var fixture = GlobberFixture.UnixLike();
                    var predicate = new Func<IFileSystemInfo, bool>(i =>
                        i.Path.FullPath != "/Working/Bar");

                    // When
                    var result = fixture.Match("/Working/Bar", predicate);

                    // Then
                    Assert.Empty(result);
                }
            }

            public sealed class WithFilePredicate
            {
                [Fact]
                public void Should_Return_Only_Files_Matching_Predicate()
                {
                    // Given
                    var fixture = GlobberFixture.UnixLike();
                    var predicate = new Func<IFile, bool>(i => i.Path.FullPath.EndsWith(".c"));

                    // When
                    var result = fixture.Match("/Working/**/*.*", null, predicate);

                    // Then
                    Assert.Equal(5, result.Length);
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Baz/Qux.c");
                    AssertEx.ContainsFilePath(result, "/Working/Bar/Qux.c");
                }
            }

            public sealed class WithDirectoryAndFilePredicate
            {
                [Fact]
                public void Should_Return_Only_Files_Matching_Predicate()
                {
                    // Given
                    var fixture = GlobberFixture.UnixLike();
                    var directoryPredicate = new Func<IFileSystemInfo, bool>(i => i.Path.FullPath.Contains("/Working"));
                    var filePredicate = new Func<IFile, bool>(i => !i.Path.FullPath.EndsWith(".dll"));

                    // When
                    var result = fixture.Match("./**/*.*", directoryPredicate, filePredicate);

                    // Then
                    Assert.Equal(10, result.Length);
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.h");
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
                    AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Baz/Qux.c");
                    AssertEx.ContainsFilePath(result, "/Working/Bar/Qux.c");
                    AssertEx.ContainsFilePath(result, "/Working/Bar/Qux.h");
                    AssertEx.ContainsFilePath(result, "/Working/foobar.rs");
                    AssertEx.ContainsFilePath(result, "/Working/foobaz.rs");
                    AssertEx.ContainsFilePath(result, "/Working/foobax.rs");
                }
            }

            [Fact]
            public void Should_Throw_If_Pattern_Is_Null()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = Record.Exception(() => fixture.Match(null));

                // Then
                AssertEx.IsArgumentNullException(result, "pattern");
            }

            [Fact]
            public void Should_Return_Empty_Result_If_Pattern_Is_Empty()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match(string.Empty);

                // Then
                Assert.Empty(result);
            }

            [Fact]
            public void Should_Return_Empty_Result_If_Pattern_Is_Invalid()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("pattern/");

                // Then
                Assert.Empty(result);
            }

            [Fact]
            public void Can_Traverse_Recursively()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/**/*.c");

                // Then
                Assert.Equal(5, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Bar/Qux.c");
            }

            [Fact]
            public void Will_Append_Relative_Root_With_Implicit_Working_Directory()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("Foo/Bar/Qux.c");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
            }

            [Fact]
            public void Should_Be_Able_To_Visit_Parent_Using_Double_Dots()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/Foo/../Foo/Bar/Qux.c");

                // Then
                Assert.Single(result);
                Assert.IsType<FilePath>(result[0]);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
            }

            [Fact]
            public void Should_Throw_If_Visiting_Parent_That_Is_Recursive_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = Record.Exception(() => fixture.Match("/Working/Foo/**/../Foo/Bar/Qux.c"));

                // Then
                Assert.NotNull(result);
                Assert.IsType<NotSupportedException>(result);
                Assert.Equal("Visiting a parent that is a recursive wildcard is not supported.", result?.Message);
            }

            [Theory]
            [InlineData("/RootFile.sh")]
            [InlineData("/Working/Foo/Bar/Qux.c")]
            public void Should_Return_Single_Path_For_Absolute_File_Path_Without_Glob_Pattern(string pattern)
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match(pattern);

                // Then
                Assert.Single(result);
                Assert.IsType<FilePath>(result[0]);
                AssertEx.ContainsFilePath(result, pattern);
            }

            [Theory]
            [InlineData("/RootDir")]
            [InlineData("/Working/Foo/Bar")]
            public void Should_Return_Single_Path_For_Absolute_Directory_Path_Without_Glob_Pattern(string pattern)
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match(pattern);

                // Then
                Assert.Single(result);
                AssertEx.ContainsDirectoryPath(result, pattern);
            }

            [Fact]
            public void Should_Return_Single_Path_For_Relative_File_Path_Without_Glob_Pattern()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                fixture.SetWorkingDirectory("/Working/Foo");

                // When
                var result = fixture.Match("./Bar/Qux.c");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
            }

            [Fact]
            public void Should_Return_Single_Path_For_Relative_Directory_Path_Without_Glob_Pattern()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                fixture.SetWorkingDirectory("/Working/Foo");

                // When
                var result = fixture.Match("./Bar");

                // Then
                Assert.Single(result);
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Bar");
            }

            [Fact]
            public void Should_Return_Files_And_Folders_For_Pattern_Ending_With_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/**/*");

                // Then
                Assert.Equal(18, result.Length);
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo");
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Bar");
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Baz");
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Bar/Baz");
                AssertEx.ContainsDirectoryPath(result, "/Working/Bar");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.h");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo.Bar.Test.dll");
                AssertEx.ContainsFilePath(result, "/Working/Bar.Qux.Test.dll");
                AssertEx.ContainsFilePath(result, "/Working/Quz.FooTest.dll");
                AssertEx.ContainsFilePath(result, "/Working/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Bar/Qux.h");
                AssertEx.ContainsFilePath(result, "/Working/foobar.rs");
                AssertEx.ContainsFilePath(result, "/Working/foobaz.rs");
                AssertEx.ContainsFilePath(result, "/Working/foobax.rs");
            }

            [Fact]
            public void Should_Return_Files_And_Folders_For_Pattern_Containing_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/Foo/*/Qux.c");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
            }

            [Fact]
            public void Should_Return_Files_And_Folders_For_Pattern_Ending_With_Character_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/Foo/Bar/Q?x.c");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
            }

            [Fact]
            public void Should_Return_Files_And_Folders_For_Pattern_Containing_Character_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/Foo/Ba?/Qux.c");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
            }

            [Fact]
            public void Should_Return_Files_For_Pattern_Ending_With_Character_Wildcard_And_Dot()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/*.Test.dll");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo.Bar.Test.dll");
                AssertEx.ContainsFilePath(result, "/Working/Bar.Qux.Test.dll");
            }

            [Fact]
            public void Should_Return_File_For_Recursive_Wildcard_Pattern_Ending_With_Wildcard_Regex()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/**/*.c");

                // Then
                Assert.Equal(5, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Bar/Qux.c");
            }

            [Fact]
            public void Should_Return_Only_Folders_For_Pattern_Ending_With_Recursive_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/**");

                // Then
                Assert.Equal(6, result.Length);
                AssertEx.ContainsDirectoryPath(result, "/Working");
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo");
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Bar");
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Baz");
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Bar/Baz");
                AssertEx.ContainsDirectoryPath(result, "/Working/Bar");
            }

            [Theory]
            [InlineData("/*.sh", "/RootFile.sh")]
            [InlineData("/Foo/*.baz", "/Foo/Bar.baz")]
            public void Should_Include_Files_In_Root_Folder_When_Using_Wildcard(string pattern, string file)
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match(pattern);

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, file);
            }

            [Theory]
            [InlineData("/**/RootFile.sh", "/RootFile.sh")]
            [InlineData("/Foo/**/Bar.baz", "/Foo/Bar.baz")]
            public void Should_Include_Files_In_Root_Folder_When_Using_Recursive_Wildcard(string pattern, string file)
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match(pattern);

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, file);
            }

            [Theory]
            [InlineData("/**/RootDir", "/RootDir")]
            [InlineData("/Foo/**/Bar", "/Foo/Bar")]
            public void Should_Include_Folder_In_Root_Folder_When_Using_Recursive_Wildcard(string pattern, string folder)
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match(pattern);

                // Then
                Assert.Single(result);
                AssertEx.ContainsDirectoryPath(result, folder);
            }

            [Fact]
            public void Should_Parse_Glob_Expressions_With_Parenthesis_In_Them()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Foo (Bar)/Baz.*");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "/Foo (Bar)/Baz.c");
            }

            [Fact]
            public void Should_Parse_Glob_Expressions_With_AtSign_In_Them()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Foo@Bar/Baz.*");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "/Foo@Bar/Baz.c");
            }

            [Fact]
            public void Should_Parse_Glob_Expressions_With_Relative_Directory_Not_At_The_Beginning()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/./*.Test.dll");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo.Bar.Test.dll");
                AssertEx.ContainsFilePath(result, "/Working/Bar.Qux.Test.dll");
            }

            [Fact]
            public void Should_Parse_Glob_Expressions_With_Unicode_Characters_And_Ending_With_Identifier()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/嵌套/**/文件.延期");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "/嵌套/目录/文件.延期");
            }

            [Fact]
            public void Should_Parse_Glob_Expressions_With_Unicode_Characters_And_Not_Ending_With_Identifier()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/嵌套/**/文件.*");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "/嵌套/目录/文件.延期");
            }

            [Fact]
            public void Should_Return_Files_And_Folders_For_Pattern_Containing_Bracket_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/fooba[rz].rs");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/foobar.rs");
                AssertEx.ContainsFilePath(result, "/Working/foobaz.rs");
            }

            [Fact]
            public void Should_Return_Files_And_Folders_For_Pattern_Containing_Brace_Expansion()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/foo{bar,bax}.rs");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/foobar.rs");
                AssertEx.ContainsFilePath(result, "/Working/foobax.rs");
            }

            [Fact]
            public void Should_Return_Files_And_Folders_For_Pattern_Containing_Negated_Bracket_Wildcard()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("/Working/fooba[!x].rs");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/foobar.rs");
                AssertEx.ContainsFilePath(result, "/Working/foobaz.rs");
            }

            [Fact]
            public void Should_Expand_Unix_Style_Environment_Variables_In_Glob_Patterns()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("WORKING_DIR", "/Working");

                // When
                var result = fixture.Match("$WORKING_DIR/Foo/Bar/Qux.c");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
            }

            [Fact]
            public void Should_Expand_Unix_Style_Environment_Variables_With_Braces_In_Glob_Patterns()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("BASE_DIR", "/Working");

                // When
                var result = fixture.Match("${BASE_DIR}/Foo/**/*.c");

                // Then
                Assert.Equal(4, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Baz/Qux.c");
            }

            [Fact]
            public void Should_Expand_Windows_Style_Environment_Variables()
            {
                // Given
                var fixture = GlobberFixture.Windows();
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("ROOT", "C:/Working");

                // When
                var result = fixture.Match("%ROOT%/Foo/Bar/Qux.c");

                // Then
                Assert.Single(result);
                AssertEx.ContainsFilePath(result, "C:/Working/Foo/Bar/Qux.c");
            }

            [Fact]
            public void Should_Expand_Multiple_Environment_Variables_In_Pattern()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("BASE", "/Working");
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("SUBDIR", "Foo");

                // When
                var result = fixture.Match("$BASE/$SUBDIR/**/*.c");

                // Then
                Assert.Equal(4, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Baz/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Baz/Qux.c");
            }

            [Fact]
            public void Should_Support_Mixed_Windows_And_Unix_Environment_Variable_Syntax()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("BASE", "/Working");
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("DIR", "Foo");

                // When
                var result = fixture.Match("$BASE/%DIR%/Bar/*.c");

                // Then
                Assert.Equal(2, result.Length);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qex.c");
            }

            [Fact]
            public void Should_Handle_Undefined_Environment_Variables_By_Leaving_Them_In_Pattern()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();

                // When
                var result = fixture.Match("$UNDEFINED_VAR/Foo/Bar/Qux.c");

                // Then
                Assert.Empty(result);
            }

            [Fact]
            public void Should_Expand_Environment_Variables_With_Recursive_Wildcards()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("WORKING", "/Working");

                // When
                var result = fixture.Match("$WORKING/**/*.c");

                // Then
                Assert.True(result.Length > 0);
                AssertEx.ContainsFilePath(result, "/Working/Foo/Bar/Qux.c");
            }

            [Fact]
            public void Should_Expand_Environment_Variables_In_Directory_Glob_Patterns()
            {
                // Given
                var fixture = GlobberFixture.UnixLike();
                ((FakeEnvironment)fixture.Environment).SetEnvironmentVariable("ROOT", "/Working");

                // When
                var result = fixture.Match("$ROOT/Foo/Bar");

                // Then
                Assert.Single(result);
                AssertEx.ContainsDirectoryPath(result, "/Working/Foo/Bar");
            }
        }
    }
}