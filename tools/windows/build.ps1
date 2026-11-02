<#
.SYNOPSIS
Builds Keystone's engine and everything under it on Windows.

.DESCRIPTION
Works on clones side by side, as docs/PLATFORMS.md lays out: Keystone, FineStructureVoxel,
game-language, finenet, FineStructureVK and finegui in one directory. GLFW and GLM, which
Windows has no package for, are built into deps\ beside them the first time.

Needs Visual Studio 2022 or later with "Desktop development with C++", the Vulkan SDK
(winget install KhronosGroup.VulkanSDK), Git for Windows and CMake 3.20 or later.

.EXAMPLE
tools\windows\build.ps1
The development build, in ..\fv-keystone, with the tests.

.EXAMPLE
tools\windows\build.ps1 -BuildDir fv-release -Gui -NoTests
What a release is made from: no console window, and nothing that isn't shipped.
#>
[CmdletBinding()]
param(
    # The directory the clones are in
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    # The engine's build directory, under $Root
    [string]$BuildDir = 'fv-keystone',
    # Build the game as a windowed program, with no console beside it
    [switch]$Gui,
    # Leave out tests and examples
    [switch]$NoTests
)

$ErrorActionPreference = 'Stop'

# A program's exit code says whether it failed. What it writes to stderr, such as CMake's warnings,
# doesn't, but Windows PowerShell makes that an error once output is redirected, as in CI.
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

function Enter-VisualStudio {
    if ($env:VCToolsInstallDir) { return }  # Already in a developer shell
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) { throw 'Visual Studio is not installed' }
    $vs = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if (-not $vs) { throw 'Visual Studio has no C++ tools: add "Desktop development with C++"' }
    $env:PATH = "$env:PATH;$(Split-Path $vswhere)"  # Its own scripts look for vswhere on PATH
    Import-Module (Join-Path $vs 'Common7\Tools\Microsoft.VisualStudio.DevShell.dll')
    Enter-VsDevShell -VsInstallPath $vs -SkipAutomaticLocation -DevCmdArguments '-arch=x64 -host_arch=x64' | Out-Null
}

function Find-VulkanSdk {
    if (-not $env:VULKAN_SDK -or -not (Test-Path $env:VULKAN_SDK)) {
        $env:VULKAN_SDK = [Environment]::GetEnvironmentVariable('VULKAN_SDK', 'Machine')
    }
    if (-not $env:VULKAN_SDK -or -not (Test-Path $env:VULKAN_SDK)) {
        $newest = Get-ChildItem 'C:\VulkanSDK' -Directory -ErrorAction SilentlyContinue |
            Sort-Object { [version]($_.Name -replace '[^0-9.]', '') } -Descending | Select-Object -First 1
        if (-not $newest) { throw 'No Vulkan SDK: winget install KhronosGroup.VulkanSDK' }
        $env:VULKAN_SDK = $newest.FullName
    }
    $env:PATH = "$(Join-Path $env:VULKAN_SDK 'Bin');$env:PATH"
    Write-Host "Vulkan SDK: $env:VULKAN_SDK"
}

$deps = Join-Path $Root 'deps'
$depsPath = $deps -replace '\\', '/'

function Build-Project {
    param([string]$Source, [string]$Binary, [string[]]$Options = @())
    Write-Host "`n== $Source" -ForegroundColor Cyan
    $configure = @('-S', (Join-Path $Root $Source), '-B', (Join-Path $Root $Binary), '-G', 'Ninja',
        '-DCMAKE_BUILD_TYPE=Release', "-DCMAKE_PREFIX_PATH=$depsPath",
        # lz4's CMake is older than CMake 4 accepts
        '-DCMAKE_POLICY_VERSION_MINIMUM=3.5') + $Options
    Invoke-Native cmake $configure
    Invoke-Native cmake @('--build', (Join-Path $Root $Binary))
}

function Get-Source {
    param([string]$Name, [string]$Url, [string]$Tag)
    $dir = Join-Path $Root "deps-src\$Name"
    if (-not (Test-Path $dir)) {
        Invoke-Native git @('-c', 'advice.detachedHead=false', 'clone', '--quiet', '--depth', '1', '--branch', $Tag, $Url, $dir) | Out-Host
    }
    return "deps-src\$Name"
}

foreach ($repo in 'Keystone', 'FineStructureVoxel', 'game-language', 'finenet', 'FineStructureVK', 'finegui') {
    if (-not (Test-Path (Join-Path $Root $repo))) { throw "$repo isn't in ${Root}: see docs/PLATFORMS.md" }
}
if (-not (Test-Path (Join-Path $Root 'finegui\third_party\imgui\imgui.h'))) {
    Invoke-Native git @('-C', (Join-Path $Root 'finegui'), 'submodule', 'update', '--init')
}

Enter-VisualStudio
Find-VulkanSdk

# GLFW and GLM, once
if (-not (Test-Path (Join-Path $deps 'lib\cmake\glfw3\glfw3Config.cmake'))) {
    $glfw = Get-Source glfw 'https://github.com/glfw/glfw.git' '3.4'
    Build-Project $glfw "$glfw\build" @("-DCMAKE_INSTALL_PREFIX=$depsPath", '-DGLFW_BUILD_EXAMPLES=OFF',
        '-DGLFW_BUILD_TESTS=OFF', '-DGLFW_BUILD_DOCS=OFF')
    Invoke-Native cmake @('--install', (Join-Path $Root "$glfw\build"))
}
if (-not (Test-Path (Join-Path $deps 'share\glm\glmConfig.cmake'))) {
    $glm = Get-Source glm 'https://github.com/g-truc/glm.git' '1.0.1'
    Build-Project $glm "$glm\build" @("-DCMAKE_INSTALL_PREFIX=$depsPath", '-DGLM_BUILD_TESTS=OFF', '-DBUILD_TESTING=OFF')
    Invoke-Native cmake @('--install', (Join-Path $Root "$glm\build"))
}

# Only ever turned off here, so that a development build's choice stands
function Off([string[]]$Names) {
    if ($NoTests) { return $Names | ForEach-Object { "-D$_=OFF" } }
    return @()
}

# In this order: each links what the ones before it built
Build-Project 'game-language' 'game-language\build' (Off 'FINESCRIPT_BUILD_TESTS')
Build-Project 'finenet' 'finenet\build' (Off 'FINENET_BUILD_TESTS')
Build-Project 'FineStructureVK' 'FineStructureVK\build' (Off 'FINEVK_BUILD_TESTS', 'FINEVK_BUILD_EXAMPLES')
Build-Project 'finegui' 'finegui\build' (Off 'FINEGUI_BUILD_TESTS', 'FINEGUI_BUILD_EXAMPLES')

$windowed = if ($Gui) { 'ON' } else { 'OFF' }
Build-Project 'FineStructureVoxel' $BuildDir (@('-DFINEVOX_BUILD_RENDER=ON', "-DFINEVOX_WINDOWS_GUI=$windowed") +
    (Off 'FINEVOX_BUILD_TESTS', 'FINEVOX_BUILD_EXAMPLES'))

$run = Join-Path $Root "$BuildDir\finevox_run.exe"
if (-not (Test-Path $run)) { throw "finevox_run.exe wasn't built: check finegui's build" }
Write-Host "`nBuilt $run" -ForegroundColor Green
