# Air Mouse Remote for Windows

**Open-source helper that makes cheap “voice air mouse” remotes actually useful for Windows dictation.**

Maintained by **[Cyber City Media](https://github.com/CYBER-CITY-MEDIA)** — built so anyone who buys the same class of remote doesn’t have to reverse-engineer HID reports and Win+H plumbing from scratch.

---

## Compatible hardware

Designed and tested against this product class:

| | |
|--|--|
| **Product** | EASYTONE Air Mouse Remote Control, 2.4 GHz Wireless Voice Remote with IR Learning |
| **Amazon** | [B08DFDNZCV](https://www.amazon.com/dp/B08DFDNZCV) |
| **Typical USB IDs** | `VID_1915&PID_1025` (includes USB audio / mic path) and/or `VID_1EA7&PID_0066` (keyboard/mouse) |
| **Also known as** | G10 / G20-style 2.4 GHz air mouse voice remotes (many white-label clones) |

> **Honest compatibility:** Listings often say “works with Windows.” That usually means **pointer + keys**. The **MIC button** was built for **Android TV / Google Assistant**, not Windows Voice Typing. This project bridges that gap.

---

## What this software does

When you press the remote’s **MIC** button on Windows:

1. Detects a **raw HID consumer “Voice Command”** report (usage **`0xCF`**, often `01-CF-00-00`) from the air mouse dongle  
2. Selects the air mouse **capture device** as the default recording mic (commonly appears as `USB Composite Device` / dual instances)  
3. Sends **`Win+H`** to open **Windows Voice Typing**  
4. Runs in the **system tray** and can start with Windows  

No custom kernel driver is required for this path.

---

## Quick start (end users)

1. Plug in the USB dongle (and batteries if needed).  
2. Run **`Install Air Mouse Remote.bat`**  
3. Read the first-run guide.  
4. Click a text field → press **MIC** on the remote → speak.  

After install, the app lives under:

`%LocalAppData%\AirMouseRemote\`

Shortcuts (optional):

- Startup: **Air Mouse Remote**  
- Desktop folder: **Audio & Voice Tools** (if present on the build machine)

---

## Critical device tip: center button = Click vs Enter

These remotes have **air-mouse lock** (gyro on/off). The **center / OK** key changes meaning:

| Air mouse mode | Center / OK button |
|----------------|--------------------|
| **Unlocked** (wave the remote → cursor moves) | **Mouse left-click** |
| **Locked** (mouse-off key; pointer does not fly) | **Enter** |

For chat/send after dictation: **lock the air mouse**, then press center for **Enter**.

Full write-up: [`docs/DEVICE-GUIDE.md`](docs/DEVICE-GUIDE.md)

---

## How we built it (technical deep dive)

### 1. The false start: keyboard remapping

Early attempts used AutoHotkey / “guess the key name” remaps (`Launch_App1`, `F13`, etc.).

**Result:** the MIC button **never appeared as a normal keyboard key**, so remaps never fired.

### 2. Discovery: raw HID, not a VK

A raw-input spy on the dongle showed:

```text
[AIR] HID data=01-CF-00-00   VID_1915&PID_1025
```

Interpretation:

- Report type: **HID** (not keyboard scan code)  
- Usage **`0xCF`**: Consumer Control **Voice Command** / application-launch voice  
- This matches “Android voice search” remotes, not Win+H  

So Windows was receiving the press — just **not as something Keyboard Manager / AHK hotkeys bind by default**.

### 3. Audio path reality

On the test PC, the dongle exposed **USB Audio** endpoints, often labeled:

- `Microphone (USB Composite Device)`  
- `Microphone (2- USB Composite Device)` ← the air mouse instance users kept unplugging/replugging  

**Not** the same as a Logitech G321 headset mic. Selecting the wrong default capture device made Win+H “open but hear nothing.”

The helper script (`app/Set-BestMic.ps1`) forces the air mouse capture device (by name preference and known endpoint GUID when available) before sending Win+H.

### 4. Why Win+H (for now)

Windows Voice Typing (`Win+H`) already:

- Handles speech-to-text with Microsoft’s engine  
- Works across most apps once a text field is focused  
- Avoids shipping a full STT stack for v1  

**Tradeoff:** Win+H sessions can dismiss when focus changes heavily. A future mode can use continuous local/cloud STT without Win+H (see roadmap). Experiments with pure `System.Speech` dictation were promising for “click anywhere and keep typing,” but the **reliable production path users hit first** was MIC → mic select → **Win+H**.

### 5. No kernel driver

A custom driver cannot invent PCM if the dongle never sends audio. For units that **do** expose USB Audio Class:

- Stock **`usbaudio`** is enough  
- Product work is **HID mapping + mic routing + UX/docs**

For units that are **keyboard-only** (`VID_1EA7` style with no capture interface), the MIC hole cannot feed Windows; button mapping alone is not enough for dictation audio.

### 6. Product packaging

So a new buyer of the **same Amazon remote** gets:

| Need | Deliverable |
|------|-------------|
| Map MIC → dictation | Background app (`AirMouseRemote.exe`) |
| Correct mic | `Set-BestMic.ps1` + defaults |
| Teaching click vs Enter | First-run UI + `docs/DEVICE-GUIDE.md` |
| One-click install | `Install Air Mouse Remote.bat` → LocalAppData + Startup + keep-alive |

---

## Architecture (v1)

```text
Air mouse dongle (2.4 GHz USB)
        │
        ├─ HID consumer reports  ──► Raw Input (usage 0xCF) ──► Fire()
        │                                                      │
        └─ USB Audio capture     ──► Set as default mic ──────┤
                                                              ▼
                                                     keybd_event Win+H
                                                              ▼
                                                   Windows Voice Typing
```

**Key implementation files:**

| Path | Role |
|------|------|
| `app/AirMouseRemote.cs` | Tray app, raw HID watcher, Win+H, first-run guide |
| `app/Set-BestMic.ps1` | Prefer air mouse capture device (AudioDeviceCmdlets) |
| `installer/Install.ps1` | Copy to `%LocalAppData%`, Startup, keep-alive |
| `docs/DEVICE-GUIDE.md` | End-user + support FAQ |
| `Install Air Mouse Remote.bat` | Double-click installer entrypoint |

### Raw Input registration (concept)

Register consumer + desktop HID pages with `RIDEV_INPUTSINK` so MIC presses are seen even when another app is focused. On `RIM_TYPEHID` from air-mouse VID/PID, scan the report for byte **`0xCF`** (edge-triggered on press).

### Win+H send (concept)

```text
LWin down → H down → H up → LWin up
```

(with a short delay after switching default mic)

---

## Build from source

Requirements: Windows, .NET Framework 4.x (`csc` from Framework64).

```powershell
cd app
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo /target:winexe `
  /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /out:AirMouseRemote.exe AirMouseRemote.cs
```

Optional dependency for mic switching: PowerShell module **AudioDeviceCmdlets**  
(`Install-Module AudioDeviceCmdlets -Scope CurrentUser`)

Then run:

```powershell
.\installer\Install.ps1
```

---

## Project layout

```text
air-mouse-remote-windows/
├── README.md
├── LICENSE
├── Install Air Mouse Remote.bat
├── app/
│   ├── AirMouseRemote.cs
│   ├── AirMouseRemote.exe      # optional prebuild
│   └── Set-BestMic.ps1
├── docs/
│   └── DEVICE-GUIDE.md
└── installer/
    └── Install.ps1
```

---

## Roadmap

- [ ] Signed installer / winget  
- [ ] Per-device profiles (more VID/PID remotes)  
- [ ] Optional continuous STT **without** Win+H  
- [ ] “Dongle connected” indicator  
- [ ] Optional rename of capture endpoint to **Air Mouse Mic** (admin)  

---

## License

MIT — see [`LICENSE`](LICENSE).

## Credits

Built at **Cyber City Media** while integrating an EASYTONE/G10 air mouse into a Windows dictation workflow for real desk/couch use — not Android TV voice search.

**Hardware reference:** [Amazon B08DFDNZCV](https://www.amazon.com/dp/B08DFDNZCV)

---

## Disclaimer

This project is not affiliated with EASYTONE, Amazon, or Microsoft. Device behavior varies by firmware clone. Audio quality and HID mappings can differ between “identical-looking” remotes.
