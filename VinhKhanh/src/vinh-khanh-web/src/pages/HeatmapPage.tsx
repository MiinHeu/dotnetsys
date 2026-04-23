import { useEffect, useRef, useState } from 'react'
import { MapContainer, TileLayer, useMap } from 'react-leaflet'
import { useQuery } from '@tanstack/react-query'
import L from 'leaflet'
import 'leaflet.heat'
import 'leaflet/dist/leaflet.css'
import { api } from '@/lib/api'
import { Clock, Zap } from 'lucide-react'

// Define the heatmap component
function HeatmapLayer({ points }: { points: Array<[number, number, number]> }) {
  const map = useMap()
  const heatmapLayerRef = useRef<any>(null)

  useEffect(() => {
    if (!map) return

    // Remove old layer if exists
    if (heatmapLayerRef.current) {
        map.removeLayer(heatmapLayerRef.current)
    }

    if (!points.length) return

    // Create heatmap layer
    const heat = (L as any).heatLayer(points, {
      radius: 30,
      blur: 20,
      maxZoom: 17,
      max: 30,
      gradient: {
        0.4: '#10b981', // Green
        0.6: '#84cc16', // Lime
        0.7: '#facc15', // Yellow
        0.8: '#f97316', // Orange
        1.0: '#ef4444', // Red
      }
    })

    heat.addTo(map)
    heatmapLayerRef.current = heat

    return () => {
      if (heatmapLayerRef.current) {
        map.removeLayer(heatmapLayerRef.current)
      }
    }
  }, [map, points])

  return null
}

export function HeatmapPage() {
  const [hours, setHours] = useState(24)

  const { data, isLoading } = useQuery({
    queryKey: ['analytics', 'heatmap', hours],
    queryFn: async () =>
      (await api.get(`/api/analytics/heatmap?hours=${hours}`)).data as Array<{
        latitude: number
        longitude: number
      }>,
    refetchInterval: hours < 1 ? 10000 : 60000, // Refresh faster for "Live" mode
  })

  const heatPoints: Array<[number, number, number]> = data?.map(p => [p.latitude, p.longitude, 1]) || []
  const center: [number, number] = [10.7535, 106.6782]

  const timeOptions = [
    { label: 'Trực tiếp', value: 0.16, icon: Zap }, // ~10 minutes
    { label: '24 giờ', value: 24, icon: Clock },
    { label: '48 giờ', value: 48, icon: Clock },
    { label: '72 giờ', value: 72, icon: Clock },
  ]

  return (
    <div className="flex flex-col h-[calc(100vh-120px)] border border-stone-200 rounded-2xl overflow-hidden bg-white shadow-xl shadow-stone-200/50">
      
      {/* Header & Controls */}
      <div className="p-5 border-b border-stone-100 flex flex-col md:flex-row md:items-center justify-between bg-white gap-4">
        <div>
          <h2 className="text-xl font-black text-stone-900 tracking-tight flex items-center gap-2">
            <span className="w-2 h-8 bg-orange-600 rounded-full"></span>
            Bản đồ nhiệt mật độ
          </h2>
          <p className="text-sm text-stone-500 font-medium">
            {hours < 1 ? 'Dữ liệu di chuyển tức thời trong 10 phút qua' : `Phân tích mật độ khách hàng trong ${hours} giờ qua`}
          </p>
        </div>

        <div className="flex bg-stone-100 p-1 rounded-xl border border-stone-200">
          {timeOptions.map((opt) => (
            <button
              key={opt.value}
              onClick={() => setHours(opt.value)}
              className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-bold transition-all ${
                hours === opt.value 
                  ? 'bg-white text-orange-600 shadow-sm ring-1 ring-stone-200' 
                  : 'text-stone-500 hover:text-stone-800'
              }`}
            >
              <opt.icon size={16} className={hours === opt.value ? 'text-orange-600' : ''} />
              {opt.label}
            </button>
          ))}
        </div>
      </div>
      
      {/* Map Area */}
      <div className="flex-1 relative z-0">
        {isLoading && (
          <div className="absolute inset-0 z-10 bg-white/40 backdrop-blur-[2px] flex items-center justify-center">
             <div className="flex flex-col items-center gap-3 bg-white p-6 rounded-2xl shadow-xl border border-stone-100">
                <div className="animate-spin rounded-full h-10 w-10 border-4 border-stone-100 border-t-orange-600"></div>
                <span className="text-sm font-bold text-stone-600">Đang tải dữ liệu...</span>
             </div>
          </div>
        )}

        {/* Legend Overlay */}
        <div className="absolute bottom-6 right-6 z-[1000] bg-white/90 backdrop-blur-md p-4 rounded-2xl shadow-2xl border border-stone-100 flex flex-col gap-3">
          <p className="text-[10px] font-black uppercase tracking-widest text-stone-400 mb-1">Mức độ đông đúc</p>
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full bg-[#10b981] shadow-sm"></span>
              <span className="text-xs font-bold text-stone-700">Thưa</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full bg-[#facc15] shadow-sm"></span>
              <span className="text-xs font-bold text-stone-700">Vừa</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full bg-[#ef4444] shadow-sm animate-pulse"></span>
              <span className="text-xs font-bold text-stone-700">Rất đông</span>
            </div>
          </div>
        </div>
        
        <MapContainer 
            center={center} 
            zoom={17} 
            className="h-full w-full"
            scrollWheelZoom={true}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <HeatmapLayer points={heatPoints} />
        </MapContainer>
      </div>
    </div>
  )
}
