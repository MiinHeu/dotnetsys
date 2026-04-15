import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { api, type Poi } from '@/lib/api'

export function Translations() {
  const [poiId, setPoiId] = useState<number | null>(null)
  const [lang, setLang] = useState('en')
  const [tName, setTName] = useState('')
  const [tDesc, setTDesc] = useState('')
  const [tAudio, setTAudio] = useState('')
  const qc = useQueryClient()

  const poisQ = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  const poiQ = useQuery({
    queryKey: ['poi', poiId],
    enabled: poiId != null,
    queryFn: async () => (await api.get<Poi>(`/api/poi/${poiId}`)).data,
  })

  const save = useMutation({
    mutationFn: async () => {
      if (poiId == null) return
      await api.post(`/api/poi/${poiId}/translation`, {
        languageCode: lang,
        name: tName,
        description: tDesc,
        audioUrl: tAudio || null,
      })
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pois'] })
      qc.invalidateQueries({ queryKey: ['poi', poiId] })
    },
  })

  useEffect(() => {
    const ex = poiQ.data?.translations?.find((x) => x.languageCode === lang)
    if (ex) {
      setTName(ex.name)
      setTDesc(ex.description)
      setTAudio(ex.audioUrl ?? '')
    } else {
      setTName('')
      setTDesc('')
      setTAudio('')
    }
  }, [poiQ.data, lang, poiId])

  return (
    <div className="vk-page mx-auto max-w-3xl">
      <section className="vk-page-header">
        <h2 className="vk-page-title">Quản lý bản dịch nội dung</h2>
        <p className="vk-page-subtitle">Biên tập tên, mô tả và audio theo từng ngôn ngữ cho mỗi quán ăn.</p>
      </section>

      <section className="vk-card p-5 md:p-6 space-y-4">
      <label className="block text-sm font-medium text-slate-700">
        Chọn POI
        <select
          className="vk-input mt-1"
          value={poiId ?? ''}
          onChange={(e) => setPoiId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">—</option>
          {poisQ.data?.map((p) => (
            <option key={p.id} value={p.id}>
              #{p.id} {p.name}
            </option>
          ))}
        </select>
      </label>

      {poiId != null && (
        <>
          <label className="block text-sm font-medium text-slate-700">
            Mã ngôn ngữ
            <input
              className="vk-input mt-1"
              value={lang}
              onChange={(e) => setLang(e.target.value)}
              placeholder="en, zh, ko…"
            />
          </label>
          {poiQ.data?.translations?.some((x) => x.languageCode === lang) && (
            <p className="text-xs text-emerald-700">Đang sửa bản dịch hiện có cho `{lang}`.</p>
          )}
          <label className="block text-sm font-medium text-slate-700">
            Tên
            <input
              className="vk-input mt-1"
              value={tName}
              onChange={(e) => setTName(e.target.value)}
              placeholder={poiQ.data?.name}
            />
          </label>
          <label className="block text-sm font-medium text-slate-700">
            Mô tả
            <textarea
              className="vk-input mt-1 min-h-28"
              rows={3}
              value={tDesc}
              onChange={(e) => setTDesc(e.target.value)}
            />
          </label>
          <label className="block text-sm font-medium text-slate-700">
            URL audio (tuỳ chọn)
            <input
              className="vk-input mt-1"
              value={tAudio}
              onChange={(e) => setTAudio(e.target.value)}
            />
          </label>
          <button
            type="button"
            className="vk-btn-primary"
            onClick={() => {
              if (!tName.trim() || !tDesc.trim()) {
                alert('Nhập tên và mô tả bản dịch.')
                return
              }
              save.mutate()
            }}
            disabled={save.isPending}
          >
            Lưu bản dịch
          </button>
        </>
      )}
      </section>
    </div>
  )
}
