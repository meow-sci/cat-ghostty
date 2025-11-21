# build

```bash
dotnet publish -c Debug -r win-x64 --self-contained true
```

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

# run (from macOS)

```bash
bin/Release/net9.0/win-x64/publish/dotnet-exe-link-libghostty.exe 2>/dev/null
```