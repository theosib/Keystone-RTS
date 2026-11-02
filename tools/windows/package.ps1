<#
.SYNOPSIS
Makes Keystone's Windows installer and portable zip from a release build.

.DESCRIPTION
Stages the game as it's installed:

    Keystone.exe            finevox_run, windowed
    shaders\*.spv
    game\                   this repo's content, as committed
    *.dll                   the C++ runtime, so there's no redistributable to install
    keystone.ico, THIRD_PARTY_NOTICES.txt

then runs Inno Setup on keystone.iss for Keystone-<version>-setup.exe, and zips the same files as
Keystone-<version>-windows-x64.zip. Both land in -OutDir.

Build first with: tools\windows\build.ps1 -BuildDir fv-release -Gui -NoTests
Inno Setup: winget install JRSoftware.InnoSetup

.EXAMPLE
tools\windows\package.ps1 -Version 0.3.0
#>
[CmdletBinding()]
param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    [string]$BuildDir = 'fv-release',
    # Defaults to what git describe says of this repo
    [string]$Version,
    [string]$OutDir,
    # Just the zip
    [switch]$NoInstaller
)

$ErrorActionPreference = 'Stop'

# Fails on the exit code alone: see build.ps1
function Invoke-Native {
    param([string]$Program, [string[]]$Arguments)
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $Program @Arguments 2>&1 | ForEach-Object { "$_" }
    } finally {
        $ErrorActionPreference = $saved
    }
    if ($LASTEXITCODE -ne 0) { throw "$Program $($Arguments -join ' ') failed with exit code $LASTEXITCODE" }
}

$keystone = Join-Path $Root 'Keystone'
$build = Join-Path $Root $BuildDir
if (-not $OutDir) { $OutDir = Join-Path $Root 'dist' }

if (-not $Version) {
    $Version = Invoke-Native git @('-C', $keystone, 'describe', '--tags', '--always', '--dirty') | Select-Object -Last 1
    if (-not $Version) { $Version = 'dev' }
}
$Version = $Version -replace '^v', ''
Write-Host "Packaging Keystone $Version from $build"

$exe = Join-Path $build 'finevox_run.exe'
if (-not (Test-Path $exe)) { throw "No $exe : run build.ps1 -BuildDir $BuildDir -Gui -NoTests first" }

# ---------------------------------------------------------------------------------------------
# Stage
# ---------------------------------------------------------------------------------------------
$stageRoot = Join-Path $OutDir 'stage'
$stage = Join-Path $stageRoot 'Keystone'
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

Copy-Item $exe (Join-Path $stage 'Keystone.exe')

New-Item -ItemType Directory -Force (Join-Path $stage 'shaders') | Out-Null
Copy-Item (Join-Path $build 'shaders\*.spv') (Join-Path $stage 'shaders')
if (-not (Test-Path (Join-Path $stage 'shaders\chunk.vert.spv'))) { throw 'No shaders: is glslc on PATH?' }

# The content as committed, so nothing local (saves, screenshots, a .venv) comes along. What's
# for working on the game rather than playing it stays behind.
$devOnly = @('.gitattributes', '.github', '.gitignore', '.mcp.json', 'CLAUDE.md', 'README.md', 'run.sh',
    'docs', 'tests', 'tools')  # As tools/package/common.sh's stage_game
$files = Invoke-Native git @('-C', $keystone, 'ls-files')
$game = Join-Path $stage 'game'
foreach ($file in $files) {
    if ($devOnly -contains ($file -split '/')[0]) { continue }
    $target = Join-Path $game ($file -replace '/', '\')
    New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
    Copy-Item (Join-Path $keystone $file) $target
}

# The C++ runtime beside the program, which Microsoft allows, rather than its redistributable
if (-not $env:VCToolsRedistDir) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $vs = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    $redist = Get-ChildItem (Join-Path $vs 'VC\Redist\MSVC') -Directory |
        Where-Object { $_.Name -match '^\d' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
    $env:VCToolsRedistDir = $redist.FullName
}
$crt = Get-ChildItem (Join-Path $env:VCToolsRedistDir 'x64') -Directory -Filter 'Microsoft.VC*.CRT' |
    Sort-Object Name -Descending | Select-Object -First 1
if (-not $crt) { throw "No C++ runtime under $env:VCToolsRedistDir" }
Copy-Item (Join-Path $crt.FullName '*.dll') $stage

# An icon made of the Shelter's, as an .ico holding the PNG as it is
$png = [IO.File]::ReadAllBytes((Join-Path $keystone 'ui\icons\shelter.png'))
$ico = New-Object IO.MemoryStream
$writer = New-Object IO.BinaryWriter($ico)
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
$writer.Write([byte]64); $writer.Write([byte]64); $writer.Write([byte]0); $writer.Write([byte]0)
$writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$png.Length); $writer.Write([uint32]22)
$writer.Write($png)
$writer.Flush()
[IO.File]::WriteAllBytes((Join-Path $stage 'keystone.ico'), $ico.ToArray())

# The game's own licence, and NOTICE where there is one: Apache 2.0 asks for both to go with a build
$licenses = Join-Path $stage 'licenses'
New-Item -ItemType Directory -Force $licenses | Out-Null
foreach ($repo in 'Keystone', 'FineStructureVoxel', 'game-language', 'finenet', 'FineStructureVK', 'finegui') {
    foreach ($name in 'LICENSE', 'LICENSE.txt', 'LICENSE.md', 'NOTICE', 'NOTICE.txt', 'NOTICE.md') {
        $source = Join-Path $Root "$repo\$name"
        if (Test-Path $source) {
            $kind = if ($name -like 'NOTICE*') { 'NOTICE' } else { 'LICENSE' }
            Copy-Item $source (Join-Path $licenses "$repo-$kind.txt")
        }
    }
}
$keystoneLicense = Join-Path $licenses 'Keystone-LICENSE.txt'
if (Test-Path $keystoneLicense) {
    Copy-Item $keystoneLicense (Join-Path $stage 'LICENSE.txt')
} else {
    Write-Warning 'Keystone has no LICENSE yet, so this build carries none of its own'
}

# What the licenses of what's linked in ask to come with it
function Get-CommentLicense([string]$Path) {
    $lines = Get-Content $Path -TotalCount 60
    $end = [Array]::IndexOf($lines, '*/')
    return ($lines[1..($end - 1)] -join "`r`n")
}
$notices = @(
    @{ Name = 'GLFW'; Text = Get-Content -Raw (Join-Path $Root 'deps-src\glfw\LICENSE.md') },
    @{ Name = 'GLM'; Text = Get-Content -Raw (Join-Path $Root 'deps-src\glm\copying.txt') },
    @{ Name = 'Dear ImGui'; Text = Get-Content -Raw (Join-Path $Root 'finegui\third_party\imgui\LICENSE.txt') },
    @{ Name = 'LZ4'; Text = Get-Content -Raw (Join-Path $build '_deps\lz4-src\lib\LICENSE') },
    @{ Name = 'JSON for Modern C++'; Text = Get-Content -Raw (Join-Path $build '_deps\nlohmann_json-src\LICENSE.MIT') },
    @{ Name = 'tinyobjloader'; Text = Get-CommentLicense (Join-Path $Root 'FineStructureVK\external\tinyobjloader\tiny_obj_loader.h') },
    @{ Name = 'stb'; Text = 'stb_image, stb_image_write and stb_truetype by Sean Barrett, in the public domain.' },
    @{ Name = 'Almendra'; Text = 'The Almendra font is under the SIL Open Font License: see game\ui\skin\Almendra-OFL.txt' }
)
$text = "Keystone includes the following, under these terms.`r`n"
foreach ($notice in $notices) {
    $text += "`r`n" + ('=' * 78) + "`r`n$($notice.Name)`r`n" + ('=' * 78) + "`r`n`r`n$($notice.Text.Trim())`r`n"
}
Set-Content -Path (Join-Path $stage 'THIRD_PARTY_NOTICES.txt') -Value $text -Encoding UTF8

# ---------------------------------------------------------------------------------------------
# Zip and installer
# ---------------------------------------------------------------------------------------------
$zip = Join-Path $OutDir "Keystone-$Version-windows-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stage -DestinationPath $zip
Write-Host "Built $zip" -ForegroundColor Green

if ($NoInstaller) { return }

$iscc = @(
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue | ForEach-Object Source),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'No Inno Setup: winget install JRSoftware.InnoSetup' }

Invoke-Native $iscc @("/DAppVersion=$Version", "/DStageDir=$stage", "/DOutputDir=$OutDir", '/Q',
    (Join-Path $PSScriptRoot 'keystone.iss'))
Write-Host "Built $(Join-Path $OutDir "Keystone-$Version-setup.exe")" -ForegroundColor Green
