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

## Windows Change Rebuild Policy (MUST)

After **any** Windows-related code change (including `Tomato.WindowsGui`, `Tomato.WindowsCore`, or Windows-specific build/runtime config), you must re-run the Windows release publish step immediately using the exact command above.

Additional requirements:
- Do not claim the Windows change is complete until the rebuild command exits successfully.
- The rebuilt output must be written to `build/windows-release`.
