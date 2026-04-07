import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { useMemo, useState } from 'react'
import { api, type Poi } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'

import { Plus, Search, MapPin } from 'lucide-react'

export function Pois() {
  const role = useAuthStore((s) => s.role)
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [ownerFilter, setOwnerFilter] = useState<number | null>(null)

  const q = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  const ownersQ = useQuery({
    queryKey: ['owners'],
    queryFn: async () => (await api.get<{ id: number; username: string }[]>('/api/auth/owners')).data,
  })

  const filtered = useMemo(() => {
    let list = q.data ?? []
    if (ownerFilter != null) {
      list = list.filter((p) => p.ownerUserId === ownerFilter)
    }
    const term = search.toLowerCase().trim()
    if (term) {
      list = list.filter(
        (p) =>
          p.name.toLowerCase().includes(term) ||
          (p.ownerInfo ?? '').toLowerCase().includes(term) ||
          (p.description ?? '').toLowerCase().includes(term),
      )
    }
    return list
  }, [q.data, search, ownerFilter])

  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm md:flex-row md:items-center">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Danh sách cơ sở</h2>
          <p className="mt-1 text-sm text-slate-500">Sổ tay các nhà hàng, quán ăn thuộc Vĩnh Khánh.</p>
        </div>

        <div className="flex flex-col items-center gap-3 sm:flex-row">
          <div className="relative w-full sm:w-64">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Tìm quán ăn..."
              className="w-full rounded-lg border border-slate-300 bg-slate-50 py-2.5 pl-10 pr-4 text-sm text-slate-900 outline-none transition-all focus:border-orange-500 focus:ring-2 focus:ring-orange-500"
            />
          </div>
          {role === 'Admin' && ownersQ.data && ownersQ.data.length > 0 && (
            <select
              value={ownerFilter?.toString() ?? ''}
              onChange={(e) => setOwnerFilter(e.target.value ? Number(e.target.value) : null)}
              className="rounded-lg border border-slate-300 bg-slate-50 px-3 py-2 text-sm text-slate-900"
            >
              <option value="">Tất cả chủ quán</option>
              {ownersQ.data.map((o) => (
                <option key={o.id} value={o.id}>{o.username}</option>
              ))}
            </select>
          )}
          {role === 'Admin' && (
            <button
              onClick={() => navigate('/pois/new')}
              className="flex w-full shrink-0 items-center justify-center gap-2 rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-bold text-white transition-colors hover:bg-orange-600 sm:w-auto"
            >
              <Plus size={18} /> Quán mới
            </button>
          )}
        </div>
      </div>

      {q.isLoading && <div className="font-medium text-slate-500">Đang tải dữ liệu...</div>}
      {q.error && <div className="font-bold text-red-500">Lỗi tải dữ liệu. Vui lòng F5 lại trang.</div>}

      <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
        {filtered.map((p) => (
          <div
            key={p.id}
            onClick={() => navigate(`/pois/${p.id}`)}
            className="flex cursor-pointer flex-col rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition-all hover:border-orange-500 hover:shadow-md"
          >
            <div className="mb-3 flex items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-600">
                <MapPin size={20} />
              </div>
              <div>
                <h3 className="line-clamp-1 text-lg font-bold text-slate-900">{p.name}</h3>
                <p className="text-xs font-semibold uppercase text-slate-500">
                  #{p.id}
                  {p.ownerInfo && (
                    <span className="ml-1 normal-case font-medium text-orange-600">· {p.ownerInfo}</span>
                  )}
                </p>
              </div>
            </div>

            <p className="mb-4 line-clamp-2 text-sm text-slate-600">
              Chạm để xem chi tiết hoặc chỉnh sửa thông tin mô tả giới thiệu quán ăn này.
            </p>

            <div className="mt-auto flex items-center justify-between border-t border-slate-100 pt-4">
              <span className="rounded bg-slate-100 px-2.5 py-1 text-xs font-bold text-slate-600">
                Ưu tiên {p.priority}
              </span>
              <span className="text-sm font-bold text-orange-600 hover:text-orange-700">Chỉnh sửa</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
