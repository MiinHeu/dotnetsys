import { useState, useEffect } from 'react'
import { api } from '@/lib/api'
import { Users, Shield, Store, Check, X, Search, RefreshCw } from 'lucide-react'

type User = {
  id: number
  username: string
  role: string
  isActive: boolean
  createdAt: string
}

export function UserManagement() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')

  async function loadUsers() {
    try {
      setLoading(true)
      const { data } = await api.get('/api/auth/users')
      setUsers(data)
    } catch (e) {
      console.error(e)
      alert('Không thể tải danh sách tài khoản. Vui lòng thử lại.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadUsers()
  }, [])

  async function toggleStatus(id: number, currentStatus: boolean, username: string) {
    if (username === 'admin') {
      alert('Không thể khóa tài khoản chính (admin).')
      return
    }

    try {
      await api.put(`/api/auth/users/${id}/status`, { isActive: !currentStatus })
      setUsers(u => u.map(x => x.id === id ? { ...x, isActive: !currentStatus } : x))
    } catch (e: any) {
      alert(e.response?.data?.message || 'Lỗi khi cập nhật trạng thái.')
    }
  }

  const filtered = users.filter(u =>
    u.username.toLowerCase().includes(search.toLowerCase()) ||
    u.role.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="max-w-6xl mx-auto space-y-6">
      <div className="flex flex-col sm:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-slate-900 mb-1">Quản Lý Người Dùng</h1>
          <p className="text-slate-500 font-medium">Theo dõi và quản lý tài khoản của admin và chủ quán.</p>
        </div>
        <button
          onClick={loadUsers}
          className="flex items-center gap-2 px-4 py-2 bg-slate-100 text-slate-600 rounded-lg font-semibold hover:bg-slate-200 transition-colors"
        >
          <RefreshCw size={18} className={loading ? 'animate-spin' : ''} /> Làm mới
        </button>
      </div>

      <div className="bg-white p-4 border border-slate-200 rounded-xl shadow-sm flex items-center gap-3">
        <Search size={20} className="text-slate-400" />
        <input
          type="text"
          placeholder="Tìm kiếm tài khoản..."
          className="bg-transparent border-none outline-none w-full text-slate-700 font-medium placeholder:font-normal"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <div className="bg-white border border-slate-200 rounded-xl shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-50 border-b border-slate-200 uppercase font-bold text-xs tracking-wider text-slate-500">
              <tr>
                <th className="px-6 py-4">ID</th>
                <th className="px-6 py-4">Tên tài khoản</th>
                <th className="px-6 py-4">Vai trò</th>
                <th className="px-6 py-4">Ngày tạo</th>
                <th className="px-6 py-4">Trạng thái</th>
                <th className="px-6 py-4 text-right">Hành động</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-slate-400">
                    <RefreshCw className="animate-spin mx-auto mb-2" /> Đang tải dữ liệu...
                  </td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-6 py-12 text-center text-slate-400 font-medium">
                    Không tìm thấy tài khoản nào.
                  </td>
                </tr>
              ) : (
                filtered.map(u => (
                  <tr key={u.id} className="hover:bg-slate-50 transition-colors">
                    <td className="px-6 py-4 font-bold text-slate-400">#{u.id}</td>
                    <td className="px-6 py-4 font-bold text-slate-800">
                      <div className="flex items-center gap-2">
                        <Users size={16} className="text-slate-400" />
                        {u.username}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      {u.role === 'Admin' ? (
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold bg-orange-100 text-orange-700">
                          <Shield size={14} /> Quản Trị Viên
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold bg-emerald-100 text-emerald-700">
                          <Store size={14} /> Chủ Quán
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {new Date(u.createdAt).toLocaleDateString('vi-VN', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                        hour: '2-digit', minute: '2-digit'
                      })}
                    </td>
                    <td className="px-6 py-4">
                      {u.isActive ? (
                        <span className="inline-flex items-center gap-1.5 text-xs font-bold text-emerald-600">
                          <Check size={16} /> Hoạt động
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 text-xs font-bold text-red-600">
                          <X size={16} /> Bị khóa
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-right">
                      {u.username !== 'admin' ? (
                        <button
                          onClick={() => toggleStatus(u.id, u.isActive, u.username)}
                          className={`px-3 py-1.5 rounded text-xs font-bold transition-colors ${u.isActive
                            ? 'bg-rose-100 text-rose-700 hover:bg-rose-200'
                            : 'bg-emerald-100 text-emerald-700 hover:bg-emerald-200'
                            }`}
                        >
                          {u.isActive ? 'Khóa' : 'Mở khóa'}
                        </button>
                      ) : (
                        <span className="text-xs text-slate-400 font-medium cursor-not-allowed">Hệ thống</span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
