$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$gameDir = Join-Path $root "game"
$exe = Join-Path $gameDir "ShengXiSurvivalDemo.exe"

if (Test-Path -LiteralPath $exe) {
    Write-Host "[shengxi-demo] 游戏已就绪：$exe"
    exit 0
}

# 用 latest/download 而不是写死版本号：发布新 Release 后这个脚本不用再改，
# 也就不会出现「tag 里的脚本还指着旧版本」这种不一致。
$url = "https://github.com/cupcakeMOON-env/shengxi-survival-demo/releases/latest/download/ShengXiSurvivalDemo-Win64.zip"
$zip = Join-Path $env:TEMP "shengxi-demo-latest.zip"

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
