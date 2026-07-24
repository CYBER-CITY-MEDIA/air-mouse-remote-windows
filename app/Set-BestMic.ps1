# Prefer the AIR MOUSE mic (renamed to "Air Mouse Mic" when possible).
# Hardware: VID_1915 dongle — was "Microphone (2- USB Composite Device)"
# GUID: {02b42cb0-e2ff-4dd2-8cff-f491143a0f88}
Import-Module AudioDeviceCmdlets -ErrorAction Stop

$rec = Get-AudioDevice -List | Where-Object { $_.Type -eq 'Recording' }

# Priority: renamed name first, then old Windows names for this endpoint
$prefs = @(
    'Air Mouse Mic',
    'Microphone (Air Mouse Mic)',
    'Microphone (2- USB Composite Device)',
    '2- USB Composite',
    'Microphone (USB Composite Device)',
    'USB Composite Device',
    'Mic Device'
)

$dev = $null
foreach ($p in $prefs) {
    $dev = $rec | Where-Object { $_.Name -eq $p -or $_.Name -like "*$p*" } | Select-Object -First 1
    if ($dev) { break }
}

# Prefer ID match if still under old name but we know the GUID
if (-not $dev) {
    $dev = $rec | Where-Object { $_.ID -match '02b42cb0-e2ff-4dd2-8cff-f491143a0f88' } | Select-Object -First 1
}

if (-not $dev) {
    Write-Output "ERROR: Air mouse mic not found. Plug in the dongle."
    exit 1
}

Set-AudioDevice -ID $dev.ID -DefaultOnly | Out-Null
Set-AudioDevice -ID $dev.ID -CommunicationOnly | Out-Null
try { Set-AudioDevice -RecordingMute $false } catch {}
try { Set-AudioDevice -RecordingCommunicationMute $false } catch {}
try { Set-AudioDevice -RecordingVolume 100 } catch {}
try { Set-AudioDevice -RecordingCommunicationVolume 100 } catch {}

$out = Join-Path $PSScriptRoot "last-mic.txt"
"$($dev.Name)|$($dev.ID)" | Set-Content $out -Encoding UTF8
Write-Output $dev.Name
