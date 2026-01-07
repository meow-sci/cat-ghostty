# CSI

## 1001 - engine ignite

echo -ne '\e[>1001;1F'

### PowerShell (paste into Windows Terminal / PowerShell)

#### Using `Write-Host` (no newline)
```powershell
Write-Host -NoNewline "`e[>1001;1F"
```

#### Using `[Console]::Write()` (no newline)
```powershell
[Console]::Write("`e[>1001;1F")
```

#### Using explicit char codes (no reliance on backtick escapes)
```powershell
[Console]::Write(([char]0x1b) + "[>1001;1F")
```

## 1002 - engine shutdown

echo -ne '\e[>1002;1F'

### PowerShell (paste into Windows Terminal / PowerShell)

#### Using `Write-Host` (no newline)
```powershell
Write-Host -NoNewline "`e[>1002;1F"
```

#### Using `[Console]::Write()` (no newline)
```powershell
[Console]::Write("`e[>1002;1F")
```

#### Using explicit char codes (no reliance on backtick escapes)
```powershell
[Console]::Write(([char]0x1b) + "[>1002;1F")
```


# OSC

## 1010 - arbitrary JSON payload

### Shortest form (echo -ne only, bash/zsh)
echo -ne '\e]1010;{"action":"engine_ignite"}\a'
echo -ne '\e]1010;{"action":"engine_shutdown"}\a'

### PowerShell (paste into Windows Terminal / PowerShell)

Notes:
- PowerShell only expands `` `e `` (ESC) and `` `a `` (BEL) inside *double quotes*.
- `Write-Output` appends a newline; prefer `Write-Host -NoNewline` or `[Console]::Write()`.

#### Using `Write-Host` (no newline)
```powershell
Write-Host -NoNewline "`e]1010;{`"action`":`"engine_ignite`"}`a"
Write-Host -NoNewline "`e]1010;{`"action`":`"engine_shutdown`"}`a"
```

#### Using `[Console]::Write()` (no newline)
```powershell
[Console]::Write("`e]1010;{`"action`":`"engine_ignite`"}`a")
[Console]::Write("`e]1010;{`"action`":`"engine_shutdown`"}`a")
```

#### Using explicit char codes (ESC + BEL)
```powershell
$esc = [char]0x1b
$bel = [char]0x07
[Console]::Write($esc + "]1010;{\"action\":\"engine_ignite\"}" + $bel)
[Console]::Write($esc + "]1010;{\"action\":\"engine_shutdown\"}" + $bel)
```

#### Alternative terminator: ST (ESC \\) instead of BEL
Some terminals accept String Terminator (ST) `ESC \` instead of BEL:
```powershell
[Console]::Write("`e]1010;{`"action`":`"engine_ignite`"}`e\\")
[Console]::Write("`e]1010;{`"action`":`"engine_shutdown`"}`e\\")
```

### Hex notation (works in printf and echo)
printf '\x1b]1010;{"action":"engine_ignite"}\a'
printf '\x1b]1010;{"action":"engine_shutdown"}\a'

### Octal notation (works everywhere)
printf '\033]1010;{"action":"engine_ignite"}\a'
printf '\033]1010;{"action":"engine_shutdown"}\a'
