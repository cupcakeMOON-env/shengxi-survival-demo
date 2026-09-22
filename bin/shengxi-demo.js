#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const { spawn } = require("child_process");

const exe = path.join(__dirname, "..", "game", "ShengXiSurvivalDemo.exe");

if (!fs.existsSync(exe)) {
  console.error("[shengxi-demo] 游戏文件不存在，请先重新执行：npm install -g https://codeload.github.com/cupcakeMOON-env/shengxi-survival-demo/tar.gz/v1.1.0");
  process.exit(1);
}

const child = spawn(exe, process.argv.slice(2), { stdio: "inherit" });
child.on("error", (err) => {
  console.error("[shengxi-demo] 启动游戏失败：", err.message);
  process.exit(1);
});
child.on("close", (code) => process.exit(code === null ? 0 : code));
