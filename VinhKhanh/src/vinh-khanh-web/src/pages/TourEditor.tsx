import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router'
import { useEffect, useState } from 'react'
import { api, type Poi, type Tour } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'

type StopRow = { poiId: number; stopOrder: number; stayMinutes: number; note?: string }

export function TourEditor() {
  const { id } = useParams()
  const isNew = id === 'new'
  const navigate = useNavigate()
  const qc = useQueryClient()
  const role = useAuthStore((s) => s.role)
  const [name, setName] = useState('')
  const [desc, setDesc] = useState('')
  const [minutes, setMinutes] = useState(60)
  const [stops, setStops] = useState<StopRow[]>([{ poiId: 1, stopOrder: 1, stayMinutes: 15 }])

  const poisQ = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  const tourQ = useQuery({
    queryKey: ['tour', id],
    enabled: !isNew && !!id,
    queryFn: async () => (await api.get<Tour>(`/api/tour/${id}?lang=vi`)).data,
  })

  useEffect(() => {
    const t = tourQ.data
    if (!t) return
    setName(t.name)
    setDesc(t.description ?? '')
    setMinutes(t.estimatedMinutes)
    const rows =
      t.stops
        ?.slice()
        .sort((a, b) => a.stopOrder - b.stopOrder)
        .map((s) => ({
          poiId: s.poi?.id ?? s.poiId ?? 0,
          stopOrder: s.stopOrder,
          stayMinutes: s.stayMinutes,
        })) ?? []
    if (rows.length) setStops(rows)
  }, [tourQ.data])

  const save = useMutation({
    mutationFn: async () => {
      const body = {
        name,
        description: desc,
        estimatedMinutes: minutes,
        stops: stops.map((s) => ({
          poiId: s.poiId,
          stopOrder: s.stopOrder,
          stayMinutes: s.stayMinutes,
          note: s.note ?? null,
        })),
      }
      if (isNew) await api.post('/api/tour', body)
      else await api.put(`/api/tour/${id}`, body)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tours'] })
      navigate('/tours')
    },
  })

  if (role !== 'Admin') {
    return <p className="text-red-600">Chỉ Admin quản lý tour.</p>
  }

  if (!isNew && tourQ.isLoading) return <p>Đang tải…</p>

  return (
    <div className="mx-auto max-w-lg space-y-4">
      <h2 className="text-lg font-semibold">{isNew ? 'Tour mới' : `Sửa tour #${id}`}</h2>
      <label className="block text-sm">
        Tên
        <input
          className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        Mô tả
        <textarea
          className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
          rows={2}
          value={desc}
          onChange={(e) => setDesc(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        Phút
        <input
          type="number"
          className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
          value={minutes}
          onChange={(e) => setMinutes(Number(e.target.value))}
        />
      </label>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium">Điểm dừng</span>
          <button
            type="button"
            className="text-sm text-orange-700"
            onClick={() =>
              setStops((s) => [
                ...s,
                { poiId: poisQ.data?.[0]?.id ?? 1, stopOrder: s.length + 1, stayMinutes: 10 },
              ])
            }
          >
            + Thêm
          </button>
        </div>
        {stops.map((row, i) => (
          <div key={i} className="flex flex-wrap gap-2 rounded border border-stone-200 p-2 dark:border-stone-700">
            <select
              className="rounded border px-1 dark:border-stone-600 dark:bg-stone-800"
              value={row.poiId}
              onChange={(e) => {
                const v = Number(e.target.value)
                setStops((prev) => prev.map((x, j) => (j === i ? { ...x, poiId: v } : x)))
              }}
            >
              {poisQ.data?.map((p) => (
                <option key={p.id} value={p.id}>
                  #{p.id} {p.name}
                </option>
              ))}
            </select>
            <input
              type="number"
              title="Thứ tự"
              className="w-16 rounded border px-1 dark:border-stone-600 dark:bg-stone-800"
              value={row.stopOrder}
              onChange={(e) => {
                const v = Number(e.target.value)
                setStops((prev) => prev.map((x, j) => (j === i ? { ...x, stopOrder: v } : x)))
              }}
            />
            <input
              type="number"
              title="Phút dừng"
              className="w-20 rounded border px-1 dark:border-stone-600 dark:bg-stone-800"
              value={row.stayMinutes}
              onChange={(e) => {
                const v = Number(e.target.value)
                setStops((prev) => prev.map((x, j) => (j === i ? { ...x, stayMinutes: v } : x)))
              }}
            />
            <button
              type="button"
              className="text-red-600"
              onClick={() => setStops((prev) => prev.filter((_, j) => j !== i))}
            >
              ×
            </button>
          </div>
        ))}
      </div>

      <div className="flex gap-2">
        <button
          type="button"
          className="rounded-lg bg-orange-600 px-4 py-2 text-white"
          onClick={() => save.mutate()}
          disabled={save.isPending}
        >
          Lưu
        </button>
        <button type="button" className="rounded-lg border px-4 py-2" onClick={() => navigate(-1)}>
          Hủy
        </button>
      </div>
    </div>
  )
}
