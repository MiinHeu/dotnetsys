const { spawn } = require('child_process')
const fs = require('fs')
const path = require('path')
const { resolveSafeWebDir } = require('./vite-path-helper.cjs')

const webDir = path.resolve(__dirname, '..')

let resolved
try {
  resolved = resolveSafeWebDir(webDir)
} catch (err) {
  console.error('[vite-dev-win] failed to prepare safe path:', err.message)
  process.exit(1)
}

const { cwd, cleanup } = resolved
const viteBin = path.join(cwd, 'node_modules', 'vite', 'bin', 'vite.js')

if (!fs.existsSync(viteBin)) {
  cleanup()
  console.error('[vite-dev-win] cannot find vite binary:', viteBin)
  process.exit(1)
}

const child = spawn(process.execPath, [viteBin], {
  stdio: 'inherit',
  cwd,
  env: { ...process.env },
})

const shutdown = (exitCode) => {
  cleanup()
  process.exit(exitCode)
}

process.on('SIGINT', () => shutdown(130))
process.on('SIGTERM', () => shutdown(143))
child.on('exit', (code) => shutdown(code ?? 0))
