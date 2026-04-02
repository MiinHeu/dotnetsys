import { useQuery } from '@tanstack/react-query'
import { api, type Poi } from '@/lib/api'
import { MapPin, Route, Users, TrendingUp, BarChart3, UserCog } from 'lucide-react'
import { useNavigate } from 'react-router'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'

export function Dashboard() {
  const navigate = useNavigate()
  
  const pois = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })
  
  const tours = useQuery({
    queryKey: ['tours', 'vi'],
    queryFn: async () => (await api.get('/api/tour?lang=vi')).data as unknown[],
  })

  // Fetch the actual Analytics Data from backend
  const analyticsTop = useQuery({
    queryKey: ['analytics', 'top'],
    queryFn: async () => (await api.get<{poiId: number; count: number; avgDuration: number}[]>('/api/analytics/top?days=30')).data,
  })

  // Merge POI names with Analytics Data
  const chartData = analyticsTop.data?.map(a => {
    const matchedPoi = pois.data?.find(p => p.id === a.poiId)
    return {
      name: matchedPoi?.name || `MÃ£ quÃ¡n #${a.poiId}`,
      count: a.count,
      duration: Math.round(a.avgDuration || 0)
    }
  }) || []

  const cards = [
    {
      label: 'QuÃ¡n Ä‚n',
      value: pois.isLoading ? 'â€¦' : pois.data?.length ?? 0,
      sub: 'Äang hoáº¡t Ä‘á»™ng',
      desc: 'CÃ¡c quÃ¡n Äƒn Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½ trÃªn há»‡ thá»‘ng',
      icon: MapPin,
      bg: 'bg-orange-600',
      emoji: 'ðŸœ',
    },
    {
      label: 'Lá»™ TrÃ¬nh',
      value: tours.isLoading ? 'â€¦' : tours.data?.length ?? 0,
      sub: 'Äang má»Ÿ',
      desc: 'Lá»™ trÃ¬nh khÃ¡m phÃ¡ áº©m thá»±c cho du khÃ¡ch',
      icon: Route,
      bg: 'bg-blue-600',
      emoji: 'ðŸ—ºï¸',
    },
    {
      label: 'KhÃ¡ch Tham Quan',
      value: '1,234',
      sub: 'Tá»•ng lÆ°á»£t',
      desc: 'LÆ°á»£t du khÃ¡ch sá»­ dá»¥ng á»©ng dá»¥ng',
      icon: Users,
      bg: 'bg-teal-600',
      emoji: 'ðŸ‘¥',
    },
    {
      label: 'LÆ°á»£t Nghe TTS',
      value: analyticsTop.isLoading ? '...' : (analyticsTop.data?.reduce((a, b) => a + b.count, 0) ?? 0),
      sub: 'Sá»‘ láº§n phÃ¡t',
      desc: 'LÆ°á»£t thuyáº¿t minh tá»± Ä‘á»™ng Ä‘Ã£ phÃ¡t',
      icon: TrendingUp,
      bg: 'bg-purple-600',
      emoji: 'ðŸŽ§',
    },
  ]

  const actions = [
    { label: 'ThÃªm QuÃ¡n Ä‚n', emoji: 'ðŸ²', path: '/pois/new' },
    { label: 'Táº¡o Lá»™ TrÃ¬nh', emoji: 'ðŸ—ºï¸', path: '/tours/new' },
    { label: 'Quáº£n LÃ½ NgÃ´n Ngá»¯', emoji: 'ðŸŒ', path: '/translations' },
    { label: 'BÃ¡o CÃ¡o Chi Tiáº¿t', emoji: 'ðŸ“Š', path: '/analytics' },
  ]

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="rounded-xl p-6 bg-white border border-slate-200 shadow-sm flex items-center justify-between">
        <div>
          <h2 className="flex items-center text-2xl font-bold text-slate-900 tracking-tight mb-1">
            <span className="mr-3 text-3xl">ðŸ“‹</span>
            Tá»•ng Quan Há»‡ Thá»‘ng
          </h2>
          <p className="text-slate-500 font-medium">Quáº£n lÃ½ ná»™i dung phá»‘ áº©m thá»±c VÄ©nh KhÃ¡nh</p>
        </div>
        <div className="text-right hidden sm:block">
          <p className="text-slate-400 text-sm font-bold uppercase tracking-widest">Cáº­p nháº­t lÃºc</p>
          <p className="text-slate-900 text-lg font-black">{new Date().toLocaleDateString('vi-VN')}</p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {cards.map(({ label, value, sub, desc, bg, emoji }) => (
          <div
            key={label}
            className={`${bg} rounded-xl p-6 text-white shadow-md hover:shadow-lg transition-shadow duration-300 flex flex-col`}
          >
            <div className="flex items-center justify-between mb-4">
              <p className="text-white/80 text-sm font-bold uppercase tracking-wider">{label}</p>
              <span className="text-2xl opacity-80">{emoji}</span>
            </div>
            <p className="text-4xl font-black mb-6">{value}</p>
            <div className="border-t border-white/20 pt-4 mt-auto">
              <p className="text-white text-sm font-bold">{sub}</p>
              <p className="text-white/60 text-xs mt-1 font-medium">{desc}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Quick Actions */}
        <div className="rounded-xl p-6 bg-white border border-slate-200 shadow-sm lg:col-span-1">
          <h3 className="text-lg font-bold text-slate-900 mb-6 flex items-center gap-2">
            <UserCog className="text-slate-400" /> Thao TÃ¡c Nhanh
          </h3>
          <div className="grid gap-3">
            {actions.map(({ label, emoji, path }) => (
              <button
                key={label}
                className="flex items-center gap-3 px-5 py-4 rounded-xl font-bold text-slate-700 bg-slate-50 border border-slate-200 hover:border-slate-900 transition-colors"
                onClick={() => navigate(path)}
              >
                <span className="text-xl">{emoji}</span>
                {label}
              </button>
            ))}
          </div>
        </div>

        {/* Real Analytics Chart */}
        <div className="rounded-xl p-6 bg-white border border-slate-200 shadow-sm lg:col-span-2 flex flex-col min-h-[350px]">
          <div className="flex justify-between items-center mb-6">
            <h3 className="text-lg font-bold text-slate-900 flex items-center gap-2">
              <BarChart3 className="text-slate-400" /> Top Äá»‹a Äiá»ƒm ÄÆ°á»£c Láº¯ng Nghe
            </h3>
            <span className="bg-slate-100 text-slate-600 px-3 py-1 text-xs font-bold rounded-full uppercase tracking-wider">30 NgÃ y Qua</span>
          </div>

          <div className="flex-1 w-full h-full">
            {analyticsTop.isLoading || pois.isLoading ? (
              <div className="w-full h-full flex items-center justify-center text-slate-500 font-bold">
                Äang táº£i dá»¯ liá»‡u biá»ƒu Ä‘á»“...
              </div>
            ) : chartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData} margin={{ top: 20, right: 0, left: -20, bottom: 0 }}>
                  <XAxis 
                    dataKey="name" 
                    stroke="#94a3b8" 
                    fontSize={12} 
                    tickLine={false} 
                    axisLine={false}
                  />
                  <YAxis 
                    stroke="#94a3b8" 
                    fontSize={12} 
                    tickLine={false} 
                    axisLine={false} 
                    tickFormatter={(value) => `${value}`}
                  />
                  <Tooltip 
                    cursor={{fill: '#f1f5f9'}}
                    contentStyle={{ borderRadius: '12px', border: 'none', boxShadow: '0 10px 15px -3px rgb(0 0 0 / 0.1)' }}
                  />
                  <Bar dataKey="count" name="Sá»‘ lÆ°á»£t nghe" fill="#0f172a" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="w-full h-full flex items-center justify-center text-slate-500 font-medium">
                ChÆ°a cÃ³ dá»¯ liá»‡u lÆ°á»£t nghe nÃ o trong 30 ngÃ y qua.
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
