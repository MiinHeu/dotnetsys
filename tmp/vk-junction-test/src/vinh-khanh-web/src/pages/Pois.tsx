import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { api, type Poi } from '@/lib/api'
import { useAuthStore } from '@/store/authStore'
import { Plus, Search, MapPin } from 'lucide-react'

export function Pois() {
  const role = useAuthStore((s) => s.role)
  const navigate = useNavigate()
  
  const q = useQuery({
    queryKey: ['pois', 'vi'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  return (
    <div className="space-y-6">
      
      {/* HEADER */}
      <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900">Danh SÃ¡ch CÆ¡ Sá»Ÿ</h2>
          <p className="text-slate-500 text-sm mt-1">Sá»• tay cÃ¡c nhÃ  hÃ ng, quÃ¡n Äƒn thuá»™c VÄ©nh KhÃ¡nh.</p>
        </div>
        
        <div className="flex flex-col sm:flex-row items-center gap-3">
          <div className="relative w-full sm:w-64">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={18} />
            <input 
              type="text" 
              placeholder="TÃ¬m quÃ¡n Äƒn..." 
              className="w-full bg-slate-50 border border-slate-300 rounded-lg py-2.5 pl-10 pr-4 text-sm focus:ring-2 focus:ring-orange-500 focus:border-orange-500 outline-none transition-all text-slate-900"
            />
          </div>
          {role === 'Admin' && (
            <button
              onClick={() => navigate('/pois/new')}
              className="w-full sm:w-auto shrink-0 flex items-center justify-center gap-2 bg-slate-900 text-white px-4 py-2.5 rounded-lg text-sm font-bold hover:bg-orange-600 transition-colors"
            >
              <Plus size={18} /> QuÃ¡n Má»›i
            </button>
          )}
        </div>
      </div>

      {q.isLoading && <div className="text-slate-500 font-medium">Äang táº£i dá»¯ liá»‡u...</div>}
      {q.error && <div className="text-red-500 font-bold">Lá»—i táº£i dá»¯ liá»‡u. Vui lÃ²ng F5 láº¡i trang.</div>}

      {/* DANH SÃCH LÆ¯á»šI CARD */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {q.data?.map((p) => (
          <div 
            key={p.id} 
            onClick={() => navigate(`/pois/${p.id}`)}
            className="flex flex-col bg-white border border-slate-200 rounded-xl p-5 shadow-sm hover:border-orange-500 hover:shadow-md cursor-pointer transition-all"
          >
            <div className="flex items-center gap-3 mb-3">
              <div className="w-10 h-10 bg-slate-100 rounded-lg flex items-center justify-center text-slate-600 shrink-0">
                <MapPin size={20} />
              </div>
              <div>
                <h3 className="font-bold text-slate-900 text-lg line-clamp-1">{p.name}</h3>
                <p className="text-xs font-semibold text-slate-500 uppercase">MÃ£ POI: #{p.id}</p>
              </div>
            </div>
            
            <p className="text-sm text-slate-600 mb-4 line-clamp-2">
              Cháº¡m Ä‘á»ƒ xem chi tiáº¿t hoáº·c chá»‰nh sá»­a thÃ´ng tin mÃ´ táº£ giá»›i thiá»‡u quÃ¡n Äƒn nÃ y.
            </p>

            <div className="mt-auto pt-4 border-t border-slate-100 flex items-center justify-between">
              <span className="text-xs font-bold bg-slate-100 text-slate-600 py-1 px-2.5 rounded">
                Æ¯u tiÃªn {p.priority}
              </span>
              <span className="text-sm font-bold text-orange-600 hover:text-orange-700">
                Chá»‰nh Sá»­a
              </span>
            </div>
          </div>
        ))}
      </div>

    </div>
  )
}
