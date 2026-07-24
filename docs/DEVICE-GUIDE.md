# EASYTONE / G10 Air Mouse — how to use it on Windows for dictation

This is the same class of remote as Amazon listing **B08DFDNZCV** (EASYTONE 2.4 GHz voice air mouse) and similar G10 / G20 clones.

## What works on Windows (honest)

| Feature | On Windows |
|--------|------------|
| Air mouse pointer | Yes |
| Keyboard keys / volume | Yes |
| Built-in MIC as a Windows microphone | Often yes (shows as USB audio / “USB Composite”) |
| MIC button → Google Assistant (Android TV) | Android only |
| MIC button → Windows Voice Typing | **Needs Air Mouse Remote software** (maps MIC → **Win+H**) |

The listing says “Windows compatible.” That means **mouse/keyboard**, not full Android-style voice search. Air Mouse Remote bridges that gap.

## One-time setup (about 2 minutes)

1. Plug in the **USB dongle**. Insert batteries if needed.
2. Run **Install Air Mouse Remote** (or open the app once).
3. Allow it to start with Windows (recommended).
4. Click into any text box → press **MIC** on the remote → speak.

Software does three things in the background:

1. Watches for the MIC button (HID **Voice Command / 0xCF** — not a normal keyboard key).
2. Selects the air mouse microphone as the Windows input device.
3. Sends **Windows + H** (Voice Typing).

## Critical: center button = Click vs Enter

This remote has **air-mouse lock** (gyro on/off). The **center / OK** key changes meaning:

| Air mouse mode | How you get there | Center / OK button |
|----------------|-------------------|--------------------|
| **Unlocked** (pointer moves when you wave) | Mouse mode ON | **Left click** |
| **Locked** (pointer does not fly around) | Stop moving + use **mouse on/off** key | **Enter** |

### Why this matters for dictation

- After dictating, you often want **Enter** to send a message or confirm.
- If the air mouse is still unlocked, center feels “broken” — it only **clicks**.
- **Lock the air mouse**, then press center → **Enter**.

Most units have a dedicated **mouse** key that toggles lock. Use it.

## Tips that make dictation feel good

1. Click the text field first (browser chat, Word, Notepad, etc.).
2. Press **MIC** once (you should hear a short beep from the software).
3. Speak clearly into the remote’s mic hole.
4. Press **MIC** again or the Voice Typing UI control to stop (Windows behavior).
5. Lock air mouse → center = Enter when you need it.

## Troubleshooting

| Problem | Fix |
|--------|-----|
| MIC does nothing | Is the dongle plugged in? Is **Air Mouse Remote** running (tray icon)? |
| Win+H opens but no speech | Check Settings → Sound → Input = air mouse / USB mic; unplug-replug dongle |
| Words go to wrong app | Click the text box again, then MIC |
| Center never types Enter | Lock air mouse (mouse off); center is click while unlocked |
| Voice only worked on Android TV | Expected without this software — install Air Mouse Remote |

## For developers / support

- USB IDs often: `VID_1915&PID_1025` (with mic) and/or `VID_1EA7&PID_0066` (kb/mouse).
- MIC button report pattern: HID consumer usage **0xCF** (e.g. `01-CF-00-00`).
- Capture endpoint may appear as `Microphone (2- USB Composite Device)` until renamed.
