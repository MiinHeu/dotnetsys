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
    <div className="mx-auto max-w-xl space-y-4">
      <h2 className="text-lg font-semibold">Bản dịch POI</h2>
      <label className="block text-sm">
        Chọn POI
        <select
          className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
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
          <label className="block text-sm">
            Mã ngôn ngữ
            <input
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={lang}
              onChange={(e) => setLang(e.target.value)}
              placeholder="en, zh, ko…"
            />
          </label>
          {poiQ.data?.translations?.some((x) => x.languageCode === lang) && (
            <p className="text-xs text-stone-500">Đang sửa bản dịch hiện có cho {lang}.</p>
          )}
          <label className="block text-sm">
            Tên
            <input
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={tName}
              onChange={(e) => setTName(e.target.value)}
              placeholder={poiQ.data?.name}
            />
          </label>
          <label className="block text-sm">
            Mô tả
            <textarea
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              rows={3}
              value={tDesc}
              onChange={(e) => setTDesc(e.target.value)}
            />
          </label>
          <label className="block text-sm">
            URL audio (tuỳ chọn)
            <input
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={tAudio}
              onChange={(e) => setTAudio(e.target.value)}
            />
          </label>
          <button
            type="button"
            className="rounded-lg bg-orange-600 px-4 py-2 text-white"
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
    </div>
  )
}
