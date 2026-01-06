┌────────────────────────────────────────────────────────────────┬──────────────┬────────┬──────────────┬──────────────┐
│ Task Name                                                      │ Total (ms)   │ Count  │ Avg (ms)     │ Avg (µs)     │
├────────────────────────────────────────────────────────────────┼──────────────┼────────┼──────────────┼──────────────┤
│ CursorRenderer.UpdateBlinkState                                │         0.04 │    60  │        0.001 │         0.58 │
│ TerminalController.Render                                      │       564.51 │    60  │        9.409 │      9408.53 │
│   TerminalUiFonts.EnsureFontsLoaded                            │         0.01 │    60  │        0.000 │         0.12 │
│   RenderTerminalCanvas                                         │       561.25 │    60  │        9.354 │      9354.10 │
│     RenderTerminalContent                                      │       561.22 │    60  │        9.354 │      9353.74 │
│       Font.Push                                                │         0.03 │    60  │        0.001 │         0.55 │
│       GetViewportRows                                          │        49.16 │    60  │        0.819 │       819.32 │
│       CellRenderingLoop                                        │       509.73 │    60  │        8.495 │      8495.48 │
│         RenderCell                                             │       469.00 │ 592800 │        0.001 │         0.79 │
│           RenderCell.Setup                                     │        29.61 │ 592800 │        0.000 │         0.05 │
│           RenderCell.ResolveColors                             │        41.05 │ 592800 │        0.000 │         0.07 │
│           StyleManager.ApplyAttributes                         │        31.92 │ 592800 │        0.000 │         0.05 │
│           RenderCell.ApplyOpacity                              │        35.02 │ 592800 │        0.000 │         0.06 │
│           Font.SelectAndRender                                 │       154.71 │ 280393 │        0.001 │         0.55 │
│             Font.SelectAndRender.SelectFont                    │        38.99 │ 280393 │        0.000 │         0.14 │
│               TerminalUiFonts.SelectFont                       │        13.20 │ 280393 │        0.000 │         0.05 │
│             Font.SelectAndRender.PushFont                      │        15.58 │ 280393 │        0.000 │         0.06 │
│             Font.SelectAndRender.AddText                       │        21.83 │ 280393 │        0.000 │         0.08 │
│             Font.SelectAndRender.PopFont                       │        14.11 │ 280393 │        0.000 │         0.05 │
│           RenderCell.DrawBackground                            │         1.92 │ 32940  │        0.000 │         0.06 │
│       RenderCursor                                             │         0.10 │    60  │        0.002 │         1.72 │
│         CursorRenderer.RenderCursor                            │         0.00 │    60  │        0.000 │         0.06 │
│       HandleMouseInput                                         │         0.21 │    60  │        0.003 │         3.43 │
└────────────────────────────────────────────────────────────────┴──────────────┴────────┴──────────────┴──────────────┘