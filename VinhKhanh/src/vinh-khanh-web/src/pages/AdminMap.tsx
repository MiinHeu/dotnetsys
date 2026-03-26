import { Fragment, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { MapContainer, TileLayer, Circle, Marker, Popup } from 'react-leaflet'
import { api, type Poi } from '@/lib/api'

export function AdminMap() {
  const q = useQuery({
    queryKey: ['pois', 'vi', 'map'],
    queryFn: async () => (await api.get<Poi[]>('/api/poi?lang=vi')).data,
  })

  const center = useMemo(() => {
    const list = q.data
    if (!list?.length) return [10.7535, 106.6783] as [number, number]
    const lat = list.reduce((s, p) => s + p.latitude, 0) / list.length
    const lon = list.reduce((s, p) => s + p.longitude, 0) / list.length
    return [lat, lon] as [number, number]
  }, [q.data])

  return (
    <div className="space-y-4">
      <h2 className="text-lg font-semibold">Bản đồ POI + bán kính geofence</h2>
      <p className="text-sm text-stone-600">
        Lưới Map X/Y chỉnh trên form POI. Vòng tròn = triggerRadiusMeters (m).
      </p>
      <div className="h-[520px] w-full overflow-hidden rounded-xl border border-stone-200 dark:border-stone-700">
        {q.isLoading && <p className="p-4">Đang tải…</p>}
        {q.data && (
          <MapContainer
            center={center}
            zoom={17}
            style={{ height: '100%', width: '100%' }}
            scrollWheelZoom
          >
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/">OSM</a>'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {q.data.map((p) => (
              <Fragment key={p.id}>
                <Circle
                  center={[p.latitude, p.longitude]}
                  radius={p.triggerRadiusMeters}
                  pathOptions={{ color: '#ea580c', fillColor: '#fdba74', fillOpacity: 0.25 }}
                />
                <Marker position={[p.latitude, p.longitude]}>
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
    </div>
  )
}
