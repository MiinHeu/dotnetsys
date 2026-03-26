import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router'
import { useEffect, useState } from 'react'
import { api, type Poi } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'

const empty: Poi = {
  id: 0,
  name: '',
  description: '',
  ownerInfo: null,
  latitude: 10.7531,
  longitude: 106.678,
  mapX: 50,
  mapY: 50,
  triggerRadiusMeters: 15,
  priority: 5,
  cooldownSeconds: 60,
  qrCode: null,
  imageUrl: null,
  audioViUrl: null,
  category: 0,
  isActive: true,
}

export function PoiEditor() {
  const { id } = useParams()
  const isNew = id === 'new'
  const navigate = useNavigate()
  const qc = useQueryClient()
  const role = useAuthStore((s) => s.role)
  const [form, setForm] = useState<Poi>(empty)

  const poiQ = useQuery({
    queryKey: ['poi', id],
    enabled: !isNew && id !== 'new' && !!id,
    queryFn: async () => (await api.get<Poi>(`/api/poi/${id}`)).data,
  })

  useEffect(() => {
    if (poiQ.data) setForm(poiQ.data)
  }, [poiQ.data])

  const save = useMutation({
    mutationFn: async () => {
      try {
        if (isNew) {
          await api.post('/api/poi', form)
        } else {
          if (!id) throw new Error('ID POI không hợp lệ')
          await api.put(`/api/poi/${id}`, form)
        }
      } catch (err) {
        console.error('POI save error:', err)
        throw err
      }
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pois'] })
      navigate('/pois')
    },
    onError: (err) => {
      console.error('Mutation error:', err)
    },
  })

  if (!isNew && poiQ.isLoading) return <p>Đang tải…</p>

  const isFormValid = form.name.trim().length > 0

  return (
    <div className="mx-auto max-w-xl space-y-4">
      <h2 className="text-lg font-semibold">{isNew ? 'POI mới' : `Sửa POI #${id}`}</h2>
      <div className="grid gap-3">
        <label className="text-sm">
          Tên
          <input
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
          />
        </label>
        <label className="text-sm">
          Mô tả
          <textarea
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            rows={3}
            value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
        </label>
        <label className="text-sm">
          Mã QR (bus / điểm dừng — duy nhất, ví dụ <span className="font-mono">VK-POI-001</span>)
          <input
            className="mt-1 w-full rounded border px-2 py-1 font-mono dark:border-stone-600 dark:bg-stone-800"
            placeholder="Để trống nếu không dùng QR"
            value={form.qrCode ?? ''}
            onChange={(e) =>
              setForm({ ...form, qrCode: e.target.value.trim() || null })
            }
          />
        </label>
        <label className="text-sm">
          Thông tin chủ quán (tùy chọn)
          <input
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.ownerInfo ?? ''}
            onChange={(e) => setForm({ ...form, ownerInfo: e.target.value || null })}
          />
        </label>
        <div className="grid grid-cols-2 gap-2">
          <label className="text-sm">
            Lat
            <input
              type="number"
              step="any"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.latitude}
              onChange={(e) => setForm({ ...form, latitude: Number(e.target.value) })}
            />
          </label>
          <label className="text-sm">
            Lon
            <input
              type="number"
              step="any"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.longitude}
              onChange={(e) => setForm({ ...form, longitude: Number(e.target.value) })}
            />
          </label>
        </div>
        <div className="grid grid-cols-2 gap-2">
          <label className="text-sm">
            Map X %
            <input
              type="number"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.mapX}
              onChange={(e) => setForm({ ...form, mapX: Number(e.target.value) })}
            />
          </label>
          <label className="text-sm">
            Map Y %
            <input
              type="number"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.mapY}
              onChange={(e) => setForm({ ...form, mapY: Number(e.target.value) })}
            />
          </label>
        </div>
        <div className="grid grid-cols-2 gap-2">
          <label className="text-sm">
            Bán kính (m)
            <input
              type="number"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.triggerRadiusMeters}
              onChange={(e) =>
                setForm({ ...form, triggerRadiusMeters: Number(e.target.value) })
              }
            />
          </label>
          <label className="text-sm">
            Ưu tiên
            <input
              type="number"
              className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
              value={form.priority}
              onChange={(e) => setForm({ ...form, priority: Number(e.target.value) })}
            />
          </label>
        </div>
        <label className="text-sm">
          Cooldown (giây)
          <input
            type="number"
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.cooldownSeconds}
            onChange={(e) => setForm({ ...form, cooldownSeconds: Number(e.target.value) })}
          />
        </label>
        <label className="text-sm">
          Ảnh URL
          <input
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.imageUrl ?? ''}
            onChange={(e) => setForm({ ...form, imageUrl: e.target.value || null })}
          />
        </label>
        <label className="text-sm">
          Audio VI URL
          <input
            className="mt-1 w-full rounded border px-2 py-1 dark:border-stone-600 dark:bg-stone-800"
            value={form.audioViUrl ?? ''}
            onChange={(e) => setForm({ ...form, audioViUrl: e.target.value || null })}
          />
        </label>
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={form.isActive}
            onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
          />
          Đang hoạt động
        </label>
        {!isNew && form.contentVersion != null && (
          <p className="text-xs text-stone-500">
            Phiên bản nội dung (đồng bộ app):{' '}
            <span className="font-mono">{form.contentVersion}</span>
          </p>
        )}
      </div>
      {save.error && (
        <div className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-700 dark:border-red-700 dark:bg-red-900/20">
          <strong>Lỗi:</strong> {(save.error as Error).message}
        </div>
      )}
      {!isFormValid && (
        <div className="rounded-lg border border-yellow-300 bg-yellow-50 p-3 text-sm text-yellow-700 dark:border-yellow-700 dark:bg-yellow-900/20">
          ⚠️ Vui lòng điền <strong>Tên</strong> POI
        </div>
      )}
      {save.isPending && (
        <p className="text-sm text-blue-600">Đang lưu...</p>
      )}
      <div className="flex gap-2">
        <button
          type="button"
          className="rounded-lg bg-orange-600 px-4 py-2 text-white disabled:opacity-50"
          onClick={() => save.mutate()}
          disabled={save.isPending || (!isNew && !id) || !isFormValid}
          title={!isFormValid ? 'Điền Tên POI trước' : ''}
        >
          Lưu
        </button>
        <button type="button" className="rounded-lg border px-4 py-2" onClick={() => navigate(-1)}>
          Hủy
        </button>
        {!isNew && role === 'Admin' && (
          <button
            type="button"
            className="ml-auto rounded-lg border border-red-300 px-4 py-2 text-red-700"
            onClick={async () => {
              if (!confirm('Vô hiệu hóa POI này?')) return
              await api.delete(`/api/poi/${id}`)
              qc.invalidateQueries({ queryKey: ['pois'] })
              navigate('/pois')
            }}
          >
            Vô hiệu hóa
          </button>
        )}
      </div>
    </div>
  )
}
