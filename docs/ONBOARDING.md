# Full onboarding — Air Mouse Remote on Windows

**Hardware:** EASYTONE 2.4 GHz wireless voice air mouse (and G10-class clones)  
**Amazon (reference listing):** https://www.amazon.com/dp/B08DFDNZCV  
**Software:** this repo — [CYBER-CITY-MEDIA/air-mouse-remote-windows](https://github.com/CYBER-CITY-MEDIA/air-mouse-remote-windows)

This is **only** about the air mouse remote (USB dongle = **mouse/keyboard + optional mic input**).  
It is **not** about headsets, Logitech G321, Voicemeeter, or other mics.

---

## What the device actually is

One USB dongle typically exposes:

| Function | What Windows sees | Works on Windows out of the box? |
|----------|-------------------|----------------------------------|
| **Air mouse / pointer** | HID mouse (gyro) | Yes |
| **Keys** (arrows, volume, OK, etc.) | HID keyboard / consumer keys | Yes |
| **Microphone hole** | Often USB audio capture (`USB Composite Device`) | Audio path yes; **MIC button does not open dictation** |
| **MIC button** | HID consumer **Voice Command (`0xCF`)** — not a normal keyboard key | Needs **this software** to map to **Win+H** |

Marketing says “Windows compatible.” That means **pointer + keys**.  
The **MIC button** was designed for **Android TV / Google Assistant**, not Windows Voice Typing. This app bridges that.

---

## 5-minute onboarding (new PC / new user)

### Step 1 — Hardware
1. Insert batteries (usually 2× AAA).  
2. Plug the **USB receiver** into the PC.  
3. Wait a few seconds. Cursor should move when you wave the remote (air mouse mode).

### Step 2 — Confirm the device in Windows
1. Open **Settings → System → Sound → Input**.  
2. Look for a capture device that appears/disappears when you unplug the dongle.  
   - Common names: `Microphone (USB Composite Device)`, `Microphone (2- USB Composite Device)`, or similar.  
3. Open **Settings → Bluetooth & devices → Devices** (or Device Manager → Human Interface Devices / Keyboards / Mice) and confirm a **2.4 GHz / HID** device is present.

Optional IDs seen in development:

- `VID_1915&PID_1025` — composite HID + audio (mic path)  
- `VID_1EA7&PID_0066` — keyboard/mouse style composite  

### Step 3 — Install Air Mouse Remote
1. Clone or download this repository.  
2. Double-click **`Install Air Mouse Remote.bat`**  
   - Or from PowerShell: `.\installer\Install.ps1`  
3. Prefer running the built app from a **user Documents path** if Windows Application Control blocks `%LocalAppData%` builds.  
4. Allow **startup** so the app runs after login (tray + taskbar).

**Dependency for automatic mic selection:**

```powershell
Install-Module AudioDeviceCmdlets -Scope CurrentUser
```

### Step 4 — First launch checklist
You should see:

- [ ] A window titled roughly **Air Mouse Remote – MIC = Win+H (RUNNING)**  
- [ ] A **taskbar** button for that window  
- [ ] A **system tray** icon (check the `^` overflow if needed)  

If the window is missing but the process is running: double-click the tray icon.

### Step 5 — First successful dictation
1. Open **Notepad** (or any text field).  
2. Click inside the text area.  
3. Press the remote’s **MIC** button once.  
4. You should hear a short beep from the software.  
5. **Windows Voice Typing** (`Win+H`) should appear.  
6. Speak into the **mic hole on the remote**.  
7. Text should appear in the focused field.

### Step 6 — Learn the center button (critical)
The **center / OK** key is **mode-dependent**:

| Air mouse mode | How you get there | Center button means |
|----------------|-------------------|---------------------|
| **Unlocked** | Gyro on — wave remote, cursor moves | **Left click** |
| **Locked** | Mouse on/off key; pointer does not fly | **Enter** |

After dictating a chat message: **lock the air mouse**, then press center for **Enter/send**.  
If center only “clicks,” you are still in unlocked air-mouse mode.

---

## Daily use (after onboarding)

1. Leave **Air Mouse Remote** running (taskbar and/or tray).  
2. Click the field you want to type into.  
3. Press **MIC** → speak.  
4. Lock air mouse when you need **Enter**.  
5. Use the remote as a normal pointer when unlocked.

---

## What the software does on each MIC press

```text
MIC button pressed
    → HID report with usage 0xCF detected (Raw Input)
    → Set Windows default recording device to air mouse capture
    → Send Win+H (Windows Voice Typing)
    → User speaks into remote mic
```

No custom kernel driver is required when the dongle already exposes USB Audio.

---

## Troubleshooting

| Symptom | What to check |
|---------|----------------|
| MIC does nothing | Is Air Mouse Remote running (taskbar/tray)? Dongle plugged in? |
| Beep but no Win+H | Focus a text field; try **Test Win+H** from tray menu |
| Win+H opens, no words | Sound → Input = air mouse / USB Composite (not another headset) |
| Center never Enter | Lock air mouse (mouse off) |
| App “disappears” | Check tray `^`; X may minimize. Use tray **Show window** |
| App won’t start from AppData | Run the `.exe` from **Documents** (Application Control can block LocalAppData) |
| Only works on Android TV | Expected without this software — install Air Mouse Remote |

---

## Support one-liner (for listings / README cards)

> Install **Air Mouse Remote**, leave it running, press **MIC** after clicking a text field.  
> Lock the air mouse when you need the center button to act as **Enter** instead of click.

---

## Not in scope

- Logitech headsets or other microphones  
- Voicemeeter routing  
- Android Google Assistant parity  
- Custom USB audio kernel drivers  

This project is **air mouse remote → Windows dictation** only.
