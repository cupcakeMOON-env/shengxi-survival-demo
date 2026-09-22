$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$gameDir = Join-Path $root "game"
$exe = Join-Path $gameDir "ShengXiSurvivalDemo.exe"
$version = "v1.1.0"

if (Test-Path -LiteralPath $exe) {
    Write-Host "[shengxi-demo] 游戏已就绪：$exe"
    exit 0
}

$url = "https://github.com/cupcakeMOON-env/shengxi-survival-demo/releases/download/$version/ShengXiSurvivalDemo-Win64.zip"
$zip = Join-Path $env:TEMP "shengxi-demo-$version.zip"

Write-Host "[shengxi-demo] 正在从 GitHub Release 下载游戏（约 33MB）..."
try {
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
}
catch {
    Write-Error "[shengxi-demo] 下载失败：$($_.Exception.Message)"
    exit 1
}

New-Item -ItemType Directory -Path $gameDir -Force | Out-Null
Expand-Archive -LiteralPath $zip -DestinationPath $gameDir -Force
Remove-Item -LiteralPath $zip -Force

if (-not (Test-Path -LiteralPath $exe)) {
    Write-Error "[shengxi-demo] 解压后未找到游戏主程序：$exe"
    exit 1
}

Write-Host "[shengxi-demo] 游戏已就绪：$exe"
