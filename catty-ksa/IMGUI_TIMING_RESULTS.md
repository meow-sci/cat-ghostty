┌────────────────────────────────────────────────────────────────┬──────────────┬─────────┬──────────────┬──────────────┐
│ Task Name                                                      │ Total (ms)   │ Count   │ Avg (ms)     │ Avg (µs)     │
├────────────────────────────────────────────────────────────────┼──────────────┼─────────┼──────────────┼──────────────┤
│ CursorRenderer.UpdateBlinkState                                │         0.03 │    60   │        0.000 │         0.42 │
│ TerminalController.Render                                      │       753.38 │    60   │       12.556 │     12556.41 │
│   TerminalUiFonts.EnsureFontsLoaded                            │         0.00 │    60   │        0.000 │         0.06 │
│   RenderTerminalCanvas                                         │       750.10 │    60   │       12.502 │     12501.74 │
│     RenderTerminalContent                                      │       750.09 │    60   │       12.501 │     12501.47 │
│       Font.Push                                                │         0.04 │    60   │        0.001 │         0.60 │
│       GetViewportRows                                          │        16.16 │    60   │        0.269 │       269.31 │
│       CellRenderingLoop                                        │       730.51 │    60   │       12.175 │     12175.17 │
│       RenderCursor                                             │         0.09 │    60   │        0.002 │         1.53 │
│       HandleMouseInput                                         │         0.31 │    60   │        0.005 │         5.20 │
│         RenderCell                                             │       688.65 │ 633600  │        0.001 │         1.09 │
│         CursorRenderer.RenderCursor                            │         0.00 │    60   │        0.000 │         0.07 │
│           RenderCell.Setup                                     │        24.39 │ 633600  │        0.000 │         0.04 │
│           RenderCell.ResolveColors                             │       310.07 │ 633600  │        0.000 │         0.49 │
│           StyleManager.ApplyAttributes                         │        26.63 │ 633600  │        0.000 │         0.04 │
│           RenderCell.ApplyOpacity                              │        29.11 │ 633600  │        0.000 │         0.05 │
│           Font.SelectAndRender                                 │       136.60 │ 284451  │        0.000 │         0.48 │
│           RenderCell.DrawBackground                            │         1.59 │ 33300   │        0.000 │         0.05 │
│             Font.SelectAndRender.SelectFont                    │        32.52 │ 284451  │        0.000 │         0.11 │
│             Font.SelectAndRender.PushFont                      │        12.94 │ 284451  │        0.000 │         0.05 │
│             Font.SelectAndRender.AddText                       │        19.97 │ 284451  │        0.000 │         0.07 │
│             Font.SelectAndRender.PopFont                       │        11.49 │ 284451  │        0.000 │         0.04 │
│             ColorResolver.Resolve                              │       221.94 │ 1267200 │        0.000 │         0.18 │
│               ColorResolver.Resolve.DefaultColor               │        81.20 │ 1020549 │        0.000 │         0.08 │
│               ColorResolver.Resolve.Named                      │        40.86 │ 246651  │        0.000 │         0.17 │
│               TerminalUiFonts.SelectFont                       │        10.17 │ 284451  │        0.000 │         0.04 │
│                 ColorResolver.ResolveNamedColor.ThemeLookup    │        20.23 │ 246651  │        0.000 │         0.08 │
└────────────────────────────────────────────────────────────────┴──────────────┴─────────┴──────────────┴──────────────┘
