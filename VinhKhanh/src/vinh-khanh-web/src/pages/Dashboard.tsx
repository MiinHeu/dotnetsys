import { useQuery } from '@tanstack/react-query'
import { api } from '@/lib/api'
import { MapPin, Route, Users, TrendingUp } from 'lucide-react'

export function Dashboard() {
  const pois = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get('/api/poi?lang=vi')).data as unknown[],
  })
  const tours = useQuery({
    queryKey: ['tours', 'vi'],
    queryFn: async () => (await api.get('/api/tour?lang=vi')).data as unknown[],
  })

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6 border border-orange-100 dark:border-gray-700">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white flex items-center">
              <TrendingUp className="h-8 w-8 mr-3 text-orange-500" />
              Tổng Quan
            </h2>
            <p className="text-gray-600 dark:text-gray-400 mt-1">Tổng quan hệ thống quản lý</p>
          </div>
          <div className="text-right">
            <p className="text-sm text-gray-500 dark:text-gray-400">Cập nhật lần cuối</p>
            <p className="text-lg font-semibold text-gray-900 dark:text-white">{new Date().toLocaleDateString('vi-VN')}</p>
          </div>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        <div className="bg-gradient-to-br from-orange-400 to-orange-600 rounded-xl shadow-lg p-6 text-white">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-orange-100 text-sm font-medium">Điểm Thú Vị</p>
              <p className="text-3xl font-bold">{pois.isLoading ? '…' : pois.data?.length ?? 0}</p>
            </div>
            <MapPin className="h-8 w-8 text-orange-200" />
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            <p className="font-medium">POI đang hoạt động</p>
            <p className="text-xs">Bao gồm các điểm ẩm thực đã được kích hoạt</p>
          </div>
        </div>

        <div className="bg-gradient-to-br from-amber-400 to-amber-600 rounded-xl shadow-lg p-6 text-white">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-amber-100 text-sm font-medium">Tour Du Lịch</p>
              <p className="text-3xl font-bold">{tours.isLoading ? '…' : tours.data?.length ?? 0}</p>
            </div>
            <Route className="h-8 w-8 text-amber-200" />
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            <p className="font-medium">Tour đang mở</p>
            <p className="text-xs">Các tour du lịch đang được kích hoạt</p>
          </div>
        </div>

        <div className="bg-gradient-to-br from-green-400 to-green-600 rounded-xl shadow-lg p-6 text-white">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-green-100 text-sm font-medium">Người Dùng</p>
              <p className="text-3xl font-bold">1,234</p>
            </div>
            <Users className="h-8 w-8 text-green-200" />
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            <p className="font-medium">Tổng người dùng</p>
            <p className="text-xs">Số lượng người dùng đang hoạt động</p>
          </div>
        </div>

        <div className="bg-gradient-to-br from-blue-400 to-blue-600 rounded-xl shadow-lg p-6 text-white">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-blue-100 text-sm font-medium">Lượt Truy Cập</p>
              <p className="text-3xl font-bold">8.5K</p>
            </div>
            <TrendingUp className="h-8 w-8 text-blue-200" />
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            <p className="font-medium">Thống kê truy cập</p>
            <p className="text-xs">Số lượt truy cập trong 30 ngày</p>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6 border border-orange-100 dark:border-gray-700">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Thao Tác Nhanh</h3>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <button className="flex items-center justify-center px-4 py-3 bg-gradient-to-r from-orange-500 to-orange-600 text-white rounded-lg hover:from-orange-600 hover:to-orange-700 transition-all duration-200">
            <MapPin className="h-5 w-5 mr-2" />
            Thêm POI
          </button>
          <button className="flex items-center justify-center px-4 py-3 bg-gradient-to-r from-amber-500 to-amber-600 text-white rounded-lg hover:from-amber-600 hover:to-amber-700 transition-all duration-200">
            <Route className="h-5 w-5 mr-2" />
            Tạo Tour
          </button>
          <button className="flex items-center justify-center px-4 py-3 bg-gradient-to-r from-green-500 to-green-600 text-white rounded-lg hover:from-green-600 hover:to-green-700 transition-all duration-200">
            <Users className="h-5 w-5 mr-2" />
            Quản Lý User
          </button>
          <button className="flex items-center justify-center px-4 py-3 bg-gradient-to-r from-blue-500 to-blue-600 text-white rounded-lg hover:from-blue-600 hover:to-blue-700 transition-all duration-200">
            <TrendingUp className="h-5 w-5 mr-2" />
            Xem Analytics
          </button>
        </div>
      </div>
    </div>
  )
}
