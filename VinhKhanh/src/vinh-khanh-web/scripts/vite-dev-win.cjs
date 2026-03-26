/**
 * Vite 8 (Rolldown) trên Windows lỗi khi project nằm trong thư mục có ký tự '#' (vd: D:\C#\...).
 * Nếu đường dẫn chứa '#': SUBST ổ ảo (mặc định V:) → root repo VinhKhanh, chạy Vite từ V:\src\vinh-khanh-web.
 *
 * Gỡ ánh xạ: subst V: /D
 */
const { spawn, execSync } = require('child_process')
const fs = require('fs')
const path = require('path')

const webDir = path.resolve(__dirname, '..')
const isWin = process.platform === 'win32'

function runVite(cwd) {
	const viteBin = path.join(cwd, 'node_modules', 'vite', 'bin', 'vite.js')
	if (!fs.existsSync(viteBin)) {
		console.error('Không tìm thấy Vite tại', viteBin)
		process.exit(1)
	}
	const p = spawn(process.execPath, [viteBin], {
		stdio: 'inherit',
		cwd,
		env: { ...process.env },
	})
	p.on('exit', (code) => process.exit(code ?? 0))
}

if (!isWin || !webDir.includes('#')) {
	runVite(webDir)
	return
}

const repoRoot = path.resolve(webDir, '..', '..')
const drive = (process.env.VK_DEV_SUBST_DRIVE || 'V:').replace(/[/\\]$/, '')
if (!/^[A-Za-z]:$/.test(drive)) {
	console.error('VK_DEV_SUBST_DRIVE phải dạng V:')
	process.exit(1)
}

const cwdOnDrive = path.join(drive, 'src', 'vinh-khanh-web')
const marker = path.join(cwdOnDrive, 'package.json')

let ok = false
try {
	console.log(`[vite-dev-win] subst ${drive} "${repoRoot}"`)
	execSync(`subst ${drive} "${repoRoot}"`, { stdio: 'inherit' })
	ok = fs.existsSync(marker)
} catch {
	if (fs.existsSync(marker)) {
		ok = true
		console.log(`[vite-dev-win] Dùng ${drive} đã ánh xạ sẵn.`)
	}
}

if (!ok) {
	console.error(
		'[vite-dev-win] Không chạy được qua subst. Thử CMD Administrator, đổi ổ (set VK_DEV_SUBST_DRIVE=W:), hoặc clone repo ra thư mục không có ký tự #.',
	)
	process.exit(1)
}

console.log(`[vite-dev-win] Chạy Vite từ ${cwdOnDrive}`)
runVite(cwdOnDrive)
