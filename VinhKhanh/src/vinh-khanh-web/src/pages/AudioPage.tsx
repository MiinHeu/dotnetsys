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
    <div className="vk-page mx-auto max-w-3xl text-left">
      <section className="vk-page-header">
        <h2 className="vk-page-title">Kho audio thuyết minh</h2>
        <p className="vk-page-subtitle">
          Upload file thu sẵn để gắn vào quán ăn hoặc bản dịch, giúp tốc độ phát ổn định hơn TTS.
        </p>
      </section>

      <section className="vk-card p-5 md:p-6 space-y-4">
        <label className="block text-sm font-medium text-slate-700">
          Ngôn ngữ file
          <input
            className="vk-input mt-1"
            value={lang}
            onChange={(e) => setLang(e.target.value)}
          />
        </label>
        <input
          className="vk-input file:mr-3 file:rounded-md file:border-0 file:bg-slate-100 file:px-3 file:py-2 file:text-sm file:font-semibold file:text-slate-700"
          type="file"
          accept=".mp3,.wav,.m4a,audio/*"
          disabled={busy}
          onChange={onFile}
        />
        {msg && <p className="rounded-lg border border-orange-200 bg-orange-50 px-3 py-2 text-sm text-orange-800">{msg}</p>}
      </section>
    </div>
  )
}
