# Project Rules

## Windows Release Packaging (MUST)

When building local Windows release executables, always use this exact command pattern:

```bash
dotnet publish Tomato.WindowsGui/Tomato.WindowsGui.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o build/windows-release \
  /p:PublishSingleFile=true \
  /p:EnableCompressionInSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:IncludeAllContentForSelfExtract=true \
  /p:DebugType=None \
  /p:DebugSymbols=false
```

Requirements:
- Keep `--self-contained true` to avoid requiring .NET runtime installation on user machines.
- Keep `EnableCompressionInSingleFile=true` to control output size.
- Keep self-extract flags for runtime compatibility with bundled native/content files.
