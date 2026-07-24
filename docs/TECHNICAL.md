# Technical notes — Air Mouse Remote on Windows

## Device under test

- **Retail:** EASYTONE 2.4 GHz voice air mouse  
- **ASIN:** [B08DFDNZCV](https://www.amazon.com/dp/B08DFDNZCV)  
- **Observed USB:** Nordic-class `VID_1915&PID_1025` (composite HID + audio), sometimes also `VID_1EA7&PID_0066` (HID only)

## MIC button

| Layer | Finding |
|-------|---------|
| AutoHotkey / Keyboard Manager | No stable named key |
| Raw Input HID | `01-CF-00-00` (and similar) |
| Meaning | Consumer **Voice Command** usage `0xCF` |

Edge-trigger on appearance of `0xCF` in the HID payload to avoid repeat spam while held.

## Capture device

Windows may expose multiple `USB Composite Device` mics. Identify the air mouse by:

1. Unplug/replug the dongle and watch which input appears/disappears  
2. Prefer names containing air-mouse labeling if renamed  
3. Known endpoint GUID observed in development:  
   `{02b42cb0-e2ff-4dd2-8cff-f491143a0f88}` as `Microphone (2- USB Composite Device)`  
   (GUIDs can differ per machine/install)

`Set-BestMic.ps1` uses **AudioDeviceCmdlets** to set default + communications capture.

## Win+H

Voice Typing requires:

- Focus in a text field  
- Dictation / online speech features enabled for the Windows account where required  
- Correct default mic  

Sending Win+H alone is insufficient if capture points at a headset or silent endpoint.

## Center key dual mode

Firmware treats center as:

- **Click** while gyro/air-mouse mode active  
- **Enter** when air-mouse mode locked off  

This is firmware UX, not a Windows bug. Document for users; do not try to “fix” with drivers.

## What we deliberately did not ship as v1

- Custom USB audio kernel driver (unnecessary when UAC audio already works)  
- Pure proprietary STT as the only path (higher complexity; Win+H works today)  
- Claiming Android Google Assistant parity on Windows  
