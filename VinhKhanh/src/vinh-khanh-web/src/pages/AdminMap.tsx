import { Fragment, useMemo, useEffect, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'
import { MapContainer, TileLayer, Circle, Marker, Popup, useMap } from 'react-leaflet'
import { api, type Poi } from '@/lib/api'
import * as L from 'leaflet'

// Suppress Leaflet error handling in React 18 StrictMode
const originalError = console.error
console.error = (...args: any[]) => {
  if (args[0]?.includes?.('removeChild')) return
  originalError(...args)
}

// Fix default icon issue with webpack
delete (L.Icon.Default.prototype as any)._getIconUrl
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
})

// Component cập nhật center bản đồ an toàn
function ChangeView({ center, zoom }: { center: [number, number], zoom: number }) {
  const map = useMap();
  useEffect(() => {
    map.setView(center, zoom);
  }, [center, zoom, map]);
  return null;
}

export function AdminMap() {
  const containerRef = useRef<HTMLDivElement>(null)
  const q = useQuery({
    queryKey: ['pois', 'vi', 'map'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  // Clean up Leaflet instance on unmount
  useEffect(() => {
    // Work around Vite path issues on Windows paths containing '#'
    // by loading Leaflet CSS from CDN instead of local asset rewrite.
    const stylesheetId = 'leaflet-cdn-style'
    if (!document.getElementById(stylesheetId)) {
      const link = document.createElement('link')
      link.id = stylesheetId
      link.rel = 'stylesheet'
      link.href = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.css'
      document.head.appendChild(link)
    }

    return () => {
      if (containerRef.current) {
        const mapContainer = containerRef.current.querySelector('.leaflet-container') as
          | (Element & { _leaflet_map?: L.Map })
          | null
        if (mapContainer?._leaflet_map) {
          try {
            mapContainer._leaflet_map.remove()
          } catch (e) {
            // Ignore cleanup errors
          }
        }
      }
    }
  }, [])

  // Center mặc định của phố Vĩnh Khánh (10.7535, 106.6783)
  const center = useMemo(() => {
    const list = q.data
    if (!list?.length) return [10.7535, 106.6783] as [number, number]
    const lat = list.reduce((s, p) => s + p.latitude, 0) / list.length
    const lon = list.reduce((s, p) => s + p.longitude, 0) / list.length
    return [lat, lon] as [number, number]
  }, [q.data])

  return (
    <div className="vk-page">
      <section className="vk-page-header">
        <h2 className="vk-page-title">Bản đồ POI và vùng kích hoạt</h2>
        <p className="vk-page-subtitle">
          Quan sát vị trí quán theo thực địa. Vòng tròn thể hiện bán kính geofence đang cấu hình cho từng điểm.
        </p>
      </section>

      <section className="vk-card p-4 md:p-5">
        <div className="mb-3 flex flex-wrap gap-3 text-xs">
          <span className="rounded-full bg-orange-50 px-3 py-1 font-semibold text-orange-700">Vòng tròn: trigger radius (m)</span>
          <span className="rounded-full bg-sky-50 px-3 py-1 font-semibold text-sky-700">Marker: vị trí quán/POI</span>
          <span className="rounded-full bg-slate-100 px-3 py-1 font-semibold text-slate-700">Map X/Y chỉnh ở trang quán ăn</span>
        </div>
        <div className="h-[560px] w-full overflow-hidden rounded-xl border border-slate-200 relative" ref={containerRef}>
        {q.isLoading && <p className="p-4 absolute z-10 bg-white shadow rounded m-4">Đang tải dữ liệu bản đồ…</p>}
        {q.data && (
          <MapContainer
            center={center}
            zoom={17}
            style={{ height: '100%', width: '100%' }}
            scrollWheelZoom
          >
            <ChangeView center={center} zoom={17} />
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/">OSM</a>'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {q.data.map((p) => (
              <Fragment key={`poi-${p.id}`}>
                <Circle
                  key={`circle-${p.id}`}
                  center={[p.latitude, p.longitude]}
                  radius={p.triggerRadiusMeters}
                  pathOptions={{ color: '#ea580c', fillColor: '#fdba74', fillOpacity: 0.25 }}
                />
                <Marker
                  key={`marker-${p.id}`}
                  position={[p.latitude, p.longitude]}
                >
                  <Popup>
                    <strong>{p.name}</strong>
                    <br />
                    Map: {p.mapX}%, {p.mapY}% · R={p.triggerRadiusMeters}m · P={p.priority}
                  </Popup>
                </Marker>
              </Fragment>
            ))}
          </MapContainer>
        )}
        </div>
      </section>
    </div>
  )
}
