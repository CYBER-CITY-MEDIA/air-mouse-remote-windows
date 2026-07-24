# Air Mouse Remote for Windows

**Make an EASYTONE / G10-class “voice air mouse” work with Windows Voice Typing (`Win+H`).**

| | |
|--|--|
| **Org** | [Cyber City Media](https://github.com/CYBER-CITY-MEDIA) |
| **Hardware** | [Amazon B08DFDNZCV — EASYTONE 2.4 GHz Voice Air Mouse](https://www.amazon.com/dp/B08DFDNZCV) |
| **License** | MIT |

> This project is **only** about that class of remote (USB dongle = pointer + keys + mic).  
> It is **not** related to Logitech headsets or other audio gear.

---

## Full onboarding (start here)

**→ [`docs/ONBOARDING.md`](docs/ONBOARDING.md)** — complete new-user flow  
**→ [`docs/DEVICE-GUIDE.md`](docs/DEVICE-GUIDE.md)** — daily use + click vs Enter  
**→ [`docs/TECHNICAL.md`](docs/TECHNICAL.md)** — HID `0xCF`, mic routing, architecture  

---

## 60-second install

1. Plug in the air mouse **USB dongle**.  
2. Run **`Install Air Mouse Remote.bat`**.  
3. Confirm the app is on the **taskbar** and in the **system tray**.  
4. Click a text box → press **MIC** on the remote → speak.

Optional (for automatic mic selection):

```powershell
Install-Module AudioDeviceCmdlets -Scope CurrentUser
```

---

## What you get

| Capability | Out of the box on Windows | With this software |
|------------|---------------------------|--------------------|
| Air mouse pointer | Yes | Yes |
| Keyboard / volume keys | Yes | Yes |
| MIC button → dictation | **No** (Android TV style HID) | **Yes** → `Win+H` |
| Remote microphone as input | Often yes (USB audio) | Forced as default before dictation |

### On each MIC press

1. Detect HID **Voice Command** (`0xCF`) from the air mouse dongle  
2. Select the air mouse **USB capture** device as default mic  
3. Send **Windows + H** (Voice Typing)  

### Center button (must teach every user)

| Mode | Center / OK |
|------|-------------|
| Air mouse **unlocked** (cursor moves) | **Click** |
| Air mouse **locked** (mouse off) | **Enter** |

---

## Device identity

- **Retail name:** EASYTONE Air Mouse / voice remote  
- **ASIN:** [B08DFDNZCV](https://www.amazon.com/dp/B08DFDNZCV)  
- **Typical USB:** `VID_1915&PID_1025` (HID + audio), sometimes `VID_1EA7&PID_0066` (HID)  
- **Capture name in Windows:** often `Microphone (… USB Composite Device)` when the mic path is present  

---

## Build

Windows + .NET Framework 4.x `csc`:

```powershell
cd app
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo /target:winexe `
  /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /out:AirMouseRemote.exe AirMouseRemote.cs
```

Then:

```powershell
.\installer\Install.ps1
```

**Note:** Some PCs block newly built EXEs under `%LocalAppData%` (Application Control).  
If the app won’t stay running, run `AirMouseRemote.exe` from a **Documents** folder path instead.

---

## Repo layout

```text
air-mouse-remote-windows/
├── README.md
├── LICENSE
├── Install Air Mouse Remote.bat
├── app/
│   ├── AirMouseRemote.cs      # tray + taskbar app, HID → Win+H
│   ├── AirMouseRemote.exe     # optional prebuild
│   └── Set-BestMic.ps1        # select air mouse capture device
├── docs/
│   ├── ONBOARDING.md          # full new-user flow
│   ├── DEVICE-GUIDE.md        # product FAQ
│   └── TECHNICAL.md           # engineering notes
└── installer/
    └── Install.ps1
```

---

## How we figured it out (short)

1. Keyboard remappers failed — MIC is **not** a normal key.  
2. Raw Input showed HID **`01-CF-00-00`** (consumer Voice Command).  
3. Dictation needs the **air mouse USB mic**, not some other recording device.  
4. **Win+H** is the reliable Windows dictation entry point for v1.  
5. Document **lock → Enter** so users don’t think the remote is broken.

Longer write-up is in the README history / `docs/TECHNICAL.md`.

---

## Disclaimer

Not affiliated with EASYTONE, Amazon, or Microsoft. Clone firmware varies.  
MIT © Cyber City Media.
