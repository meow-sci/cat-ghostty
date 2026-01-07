# CSI

## 1001 - engine ignite

echo -ne '\e[>1001;1F'

## 1002 - engine shutdown

echo -ne '\e[>1002;1F'


# OSC

## 1010 - arbitrary JSON payload

### Shortest form (echo -ne only, bash/zsh)
echo -ne '\e]1010;{"action":"engine_ignite"}\a'
echo -ne '\e]1010;{"action":"engine_shutdown"}\a'

### Hex notation (works in printf and echo)
printf '\x1b]1010;{"action":"engine_ignite"}\a'
printf '\x1b]1010;{"action":"engine_shutdown"}\a'

### Octal notation (works everywhere)
printf '\033]1010;{"action":"engine_ignite"}\a'
printf '\033]1010;{"action":"engine_shutdown"}\a'
