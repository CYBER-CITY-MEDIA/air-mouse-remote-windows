# Technical notes

## Scope

Air mouse remote only (`B08DFDNZCV` class). No headset/G321 code paths.

## MIC button

- Raw HID consumer usage **`0xCF`** (often payload `01-CF-00-00`)  
- VID/PID filters: `1915`/`1025`, `1EA7`/`0066`  
- Edge-trigger on press; map to mic select + `Win+H`

## Capture

Prefer USB composite capture tied to the air mouse dongle (name varies).  
`Set-BestMic.ps1` uses AudioDeviceCmdlets; never selects unrelated headsets by design of name priority (USB Composite / Air Mouse / Mic Device).

## Why not a kernel driver

If USB Audio Class is present, `usbaudio` is enough. Product work is HID mapping + routing + docs.

## UI requirements

App must show **taskbar** (`ShowInTaskbar = true`) and **tray** (`NotifyIcon`).  
Prefer launching from a Documents path if Application Control blocks LocalAppData.
