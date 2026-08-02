# Codex World — auto demo recorder
# Launches the built game, records the screen while it runs, stops when the game exits.
#   powershell -ExecutionPolicy Bypass -File Tools\record-demo.ps1
#   ...-File Tools\record-demo.ps1 -Mic          # also record narration from the mic
#
# Game audio is NOT captured: this machine exposes no loopback ("Stereo Mix") device,
# and ffmpeg on Windows can only read real capture devices. Use Game Bar (Win+Alt+R)
# if the recording needs the game's music and SFX.

param(
    [string]$Exe = "$PSScriptRoot\..\Build\Windows\FYP-CodexWorld.exe",
    [string]$OutDir = "$PSScriptRoot\..\Recordings",
    [int]$Fps = 30,
    [switch]$Mic
)

$ErrorActionPreference = "Stop"

$ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue).Source
if (-not $ffmpeg) { throw "ffmpeg not on PATH." }
if (-not (Test-Path $Exe)) { throw "Game exe not found: $Exe  (build it first)" }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

$out = Join-Path $OutDir ("demo-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".mp4")

# gdigrab captures the whole desktop; run the game fullscreen (or maximised) so the
# frame is all game. Capturing by window title breaks whenever the title changes.
$ffArgs = @("-hide_banner", "-loglevel", "warning", "-y",
            "-f", "gdigrab", "-framerate", "$Fps", "-i", "desktop")
if ($Mic) {
    $ffArgs += @("-f", "dshow", "-i", "audio=Microphone Array (Realtek(R) Audio)",
                 "-c:a", "aac", "-b:a", "160k")
}
$ffArgs += @("-c:v", "libx264", "-preset", "veryfast", "-crf", "23",
             "-pix_fmt", "yuv420p", "-movflags", "+faststart", $out)

Write-Host "Launching game..."
$game = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds 3   # let the window come up before the first frame

Write-Host "Recording -> $out"
Write-Host "Stop by quitting the game (EXIT in the menu, or Alt+F4)."

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $ffmpeg
$psi.Arguments = ($ffArgs | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' '
$psi.RedirectStandardInput = $true
$psi.UseShellExecute = $false
$rec = [System.Diagnostics.Process]::Start($psi)

try {
    $game.WaitForExit()
} finally {
    # "q" tells ffmpeg to finalise the file; killing it would leave an unplayable mp4.
    if (-not $rec.HasExited) {
        $rec.StandardInput.WriteLine("q")
        if (-not $rec.WaitForExit(10000)) { $rec.Kill() }
    }
}

$size = [math]::Round((Get-Item $out).Length / 1MB, 1)
Write-Host "Saved $out ($size MB)"
