import { useEffect, useRef } from 'react'
import { MapContainer, TileLayer, useMap } from 'react-leaflet'
import { useQuery } from '@tanstack/react-query'
import L from 'leaflet'
import 'leaflet.heat'
import 'leaflet/dist/leaflet.css'
import { api } from '@/lib/api'

// Define the heatmap component
function HeatmapLayer({ points }: { points: Array<[number, number, number]> }) {
  const map = useMap()
  const heatmapLayerRef = useRef<any>(null)

  useEffect(() => {
    if (!map || !points.length) return

    // Remove old layer if exists
    if (heatmapLayerRef.current) {
        map.removeLayer(heatmapLayerRef.current)
    }

    // Create heatmap layer
    // Gradient: Green -> Lime -> Yellow -> Orange -> Red
    const heat = (L as any).heatLayer(points, {
      radius: 30, // Tăng nhẹ radius để mượt hơn
      blur: 20,
      maxZoom: 17,
      max: 30, // Yêu cầu ít nhất 30 điểm trùng lặp để đạt màu Đỏ
      gradient: {
        0.4: '#10b981', // Green (Emerald 500)
        0.6: '#84cc16', // Lime 500
        0.7: '#facc15', // Yellow 400
        0.8: '#f97316', // Orange 500
        1.0: '#ef4444', // Red 500
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
  const { data, isLoading } = useQuery({
    queryKey: ['analytics', 'heatmap', 'full'],
    queryFn: async () =>
      (await api.get('/api/analytics/heatmap?hours=72')).data as Array<{
        latitude: number
        longitude: number
      }>,
    refetchInterval: 30000, // Refresh every 30s
  })

  // Format data for leaflet-heat: [lat, lng, intensity]
  const heatPoints: Array<[number, number, number]> = data?.map(p => [p.latitude, p.longitude, 1]) || []

  const center: [number, number] = [10.7535, 106.6782]

  return (
    <div className="flex flex-col h-[calc(100vh-120px)] border border-stone-200 rounded-xl overflow-hidden bg-white shadow-sm">
      <div className="p-4 border-b border-stone-100 flex items-center justify-between bg-stone-50/50">
        <div>
          <h2 className="text-lg font-bold text-stone-800">Bản đồ nhiệt (Heatmap)</h2>
          <p className="text-xs text-stone-500">Mật độ di chuyển khách hàng trong 72 giờ qua</p>
        </div>
        <div className="flex items-center gap-3 text-xs">
          <div className="flex items-center gap-1">
            <span className="w-3 h-3 rounded-full bg-[#10b981]"></span>
            <span>Thưa</span>
          </div>
          <div className="flex items-center gap-1">
            <span className="w-3 h-3 rounded-full bg-[#facc15]"></span>
            <span>Vừa</span>
          </div>
          <div className="flex items-center gap-1">
            <span className="w-3 h-3 rounded-full bg-[#ef4444]"></span>
            <span>Đông</span>
          </div>
        </div>
      </div>
      
      <div className="flex-1 relative z-0">
        {isLoading && (
          <div className="absolute inset-0 z-10 bg-white/50 flex items-center justify-center">
             <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-stone-800"></div>
          </div>
        )}
        
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
