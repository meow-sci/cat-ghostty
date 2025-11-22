
# run KeyDemo.cs test to see if the libghostty encoder is working correctly from managed C# code

```bash
dotnet run -- --key-demo
```

Compared output from various tests in the encode.html known example from the Ghostty repo, appears to be working correctly

# build

```bash
dotnet publish -c Debug -r win-x64 --self-contained true
```

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```
