import { useState } from 'react'
import { api } from '@/lib/api'

export function AudioPage() {
  const [lang, setLang] = useState('vi')
  const [msg, setMsg] = useState('')
  const [busy, setBusy] = useState(false)

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setBusy(true)
    setMsg('')
    try {
      const fd = new FormData()
      fd.append('file', file)
      fd.append('lang', lang)
      const { data } = await api.post('/api/audio/upload', fd)
      setMsg(`Đã upload: ${data.url ?? data.filename ?? 'OK'}`)
    } catch {
      setMsg('Upload thất bại (cần quyền Admin/Owner).')
    } finally {
      setBusy(false)
      e.target.value = ''
    }
  }

  return (
    <div className="mx-auto max-w-md space-y-4 text-left">
      <h2 className="text-lg font-semibold">Upload audio thu sẵn</h2>
      <p className="text-sm text-stone-600">
        mp3 / wav / m4a. Sau khi upload, dán URL vào POI hoặc bản dịch.
      </p>
      <label className="block text-sm">
        Ngôn ngữ file
        <input
          className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
          value={lang}
          onChange={(e) => setLang(e.target.value)}
        />
      </label>
      <input type="file" accept=".mp3,.wav,.m4a,audio/*" disabled={busy} onChange={onFile} />
      {msg && <p className="text-sm text-orange-800 dark:text-orange-200">{msg}</p>}
    </div>
  )
}
