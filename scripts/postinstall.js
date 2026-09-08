const path = require("path");
const { spawnSync } = require("child_process");

const ps1 = path.join(__dirname, "install-game.ps1");
const result = spawnSync(
  "powershell",
  ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ps1],
  { stdio: "inherit" }
);

if (result.status !== 0) {
  console.error("[rts-demo] 游戏下载安装失败，请检查网络后重试。");
  process.exit(result.status === null ? 1 : result.status);
}
