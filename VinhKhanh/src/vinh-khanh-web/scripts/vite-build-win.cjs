const { execSync, spawnSync } = require('child_process')
const fs = require('fs')
const os = require('os')
const path = require('path')

const sourceDir = path.resolve(__dirname, '..')
const isWinHashPath = process.platform === 'win32' && sourceDir.includes('#')

function removeDirSafe(dirPath) {
  if (!fs.existsSync(dirPath)) return
  fs.rmSync(dirPath, { recursive: true, force: true })
}

function runStep(cwd, command, args, label) {
  const result = spawnSync(command, args, {
    cwd,
    stdio: 'inherit',
    env: { ...process.env },
  })

  if (result.status !== 0) {
    throw new Error(`${label} failed`)
  }
}

function prepareBuildDir() {
  if (!isWinHashPath) {
    return {
      cwd: sourceDir,
      commitDist: () => {},
      cleanup: () => {},
    }
  }

  const tempDir = path.join(os.tmpdir(), `vk-web-build-${process.pid}`)
  removeDirSafe(tempDir)
  fs.mkdirSync(tempDir, { recursive: true })

  fs.cpSync(sourceDir, tempDir, {
    recursive: true,
    force: true,
    filter: (src) => {
      const name = path.basename(src)
      if (name === 'node_modules' || name === 'dist') return false
      return true
    },
  })

  const tempNodeModules = path.join(tempDir, 'node_modules')
  execSync(`cmd /c mklink /J "${tempNodeModules}" "${path.join(sourceDir, 'node_modules')}"`, { stdio: 'ignore' })

  return {
    cwd: tempDir,
    commitDist: () => {
      const fromDist = path.join(tempDir, 'dist')
      const toDist = path.join(sourceDir, 'dist')
      if (!fs.existsSync(fromDist)) {
        throw new Error('dist folder not found after build')
      }
      removeDirSafe(toDist)
      fs.cpSync(fromDist, toDist, { recursive: true, force: true })
    },
    cleanup: () => removeDirSafe(tempDir),
  }
}

const tscBin = path.join(sourceDir, 'node_modules', 'typescript', 'bin', 'tsc')
const viteBin = path.join(sourceDir, 'node_modules', 'vite', 'bin', 'vite.js')

if (!fs.existsSync(tscBin) || !fs.existsSync(viteBin)) {
  console.error('[vite-build-win] missing local binaries (tsc or vite). Run npm install first.')
  process.exit(1)
}

let context
try {
  context = prepareBuildDir()
  runStep(context.cwd, process.execPath, [path.join(context.cwd, 'node_modules', 'typescript', 'bin', 'tsc'), '-b'], 'tsc')
  runStep(context.cwd, process.execPath, [path.join(context.cwd, 'node_modules', 'vite', 'bin', 'vite.js'), 'build'], 'vite build')
  context.commitDist()
  context.cleanup()
} catch (err) {
  if (context) context.cleanup()
  console.error('[vite-build-win]', err.message)
  process.exit(1)
}
