const { execSync } = require('child_process')
const fs = require('fs')
const os = require('os')
const path = require('path')

function safeRemoveDir(dirPath) {
  if (!fs.existsSync(dirPath)) return
  try {
    execSync(`cmd /c rmdir "${dirPath}"`, { stdio: 'ignore' })
  } catch {
    // ignore cleanup errors
  }
}

function createJunction(targetDir) {
  const linkPath = path.join(os.tmpdir(), `vk-vite-link-${process.pid}`)
  safeRemoveDir(linkPath)
  execSync(`cmd /c mklink /J "${linkPath}" "${targetDir}"`, { stdio: 'ignore' })
  return linkPath
}

function resolveSafeWebDir(webDir) {
  if (process.platform !== 'win32' || !webDir.includes('#')) {
    return { cwd: webDir, cleanup: () => {} }
  }

  const repoRoot = path.resolve(webDir, '..', '..')
  const linkPath = createJunction(repoRoot)
  const linkedWebDir = path.join(linkPath, 'src', 'vinh-khanh-web')

  if (!fs.existsSync(path.join(linkedWebDir, 'package.json'))) {
    safeRemoveDir(linkPath)
    throw new Error(`Cannot map web dir through junction: ${linkedWebDir}`)
  }

  return {
    cwd: linkedWebDir,
    cleanup: () => safeRemoveDir(linkPath),
  }
}

module.exports = { resolveSafeWebDir }
