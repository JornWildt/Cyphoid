# Developing Cyphoid

The solution contains the Cyphoid library and its test project. The library
targets .NET 10. Backend integration is described in [BACKEND_GUIDE.md](BACKEND_GUIDE.md);
query behavior is described in [CYPHER_SUPPORT.md](CYPHER_SUPPORT.md).

## Build and test

From the repository root:

```shell
dotnet restore Cyphoid.slnx
dotnet build Cyphoid.slnx
dotnet test Cyphoid.slnx
```

## Prepare a release

1. Set `Version` and `PackageReleaseNotes` in `Src/Cyphoid.Core/Cyphoid.Core.csproj`.
2. Record the release version, date, and changes in [CHANGES.md](CHANGES.md).
3. Run the Release test suite:

   ```shell
   dotnet test Cyphoid.slnx -c Release
   ```

Release builds generate the NuGet package and symbol package under
`Src/Cyphoid.Core/bin/Release/`: `Cyphoid.<version>.nupkg` and
`Cyphoid.<version>.snupkg`. Verify their metadata and packaged documentation before
publishing. Upload the packages to your NuGet feed as a separate publishing step.

## Generate the ANTLR parser

The grammar is `Src/Cyphoid.Core/Antlr/Cypher.g4`. Generated C# lexer, parser,
listener, and visitor files live in its `Generated` directory and are committed
to the repository. Normal .NET builds use these files without running Java.

To regenerate them, install Java and download the ANTLR 4.13.2 complete JAR.
See [ANTLR installation and usage](https://github.com/antlr/antlr4/blob/master/doc/getting-started.md)
and [ANTLR downloads](https://www.antlr.org/download.html).

Run this command from `Src/Cyphoid.Core/Antlr`, adjusting the JAR path for your machine:

```shell
java -jar D:/Javalib/antlr-4.13.2-complete.jar -Dlanguage=CSharp -visitor -o Generated Cypher.g4
```

The explicit JAR path avoids requiring a CLASSPATH setting. Alternatively, add
the complete JAR to CLASSPATH and invoke the tool with:

```shell
java org.antlr.v4.Tool -Dlanguage=CSharp -visitor -o Generated Cypher.g4
```

Review generated changes alongside the grammar and AST visitor changes. Check
formatting with `git diff --check` and run the test suite after regeneration.
