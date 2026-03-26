import { Navigate, Route, Routes } from 'react-router'
import { Layout } from '@/components/Layout'
import { Login } from '@/pages/Login'
import { Dashboard } from '@/pages/Dashboard'
import { Pois } from '@/pages/Pois'
import { PoiEditor } from '@/pages/PoiEditor'
import { AdminMap } from '@/pages/AdminMap'
import { ToursAdmin } from '@/pages/ToursAdmin'
import { TourEditor } from '@/pages/TourEditor'
import { Translations } from '@/pages/Translations'
import { AudioPage } from '@/pages/AudioPage'
import { AnalyticsPage } from '@/pages/AnalyticsPage'
import { HistoryPage } from '@/pages/HistoryPage'
import { useAuthStore } from '@/store/authStore'

function LoginGate() {
  const token = useAuthStore((s) => s.token)
  if (token) return <Navigate to="/" replace />
  return <Login />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginGate />} />
      <Route element={<Layout />}>
        <Route path="/" element={<Dashboard />} />
        <Route path="/pois" element={<Pois />} />
        <Route path="/pois/:id" element={<PoiEditor />} />
        <Route path="/map" element={<AdminMap />} />
        <Route path="/tours" element={<ToursAdmin />} />
        <Route path="/tours/:id" element={<TourEditor />} />
        <Route path="/translations" element={<Translations />} />
        <Route path="/audio" element={<AudioPage />} />
        <Route path="/analytics" element={<AnalyticsPage />} />
        <Route path="/history" element={<HistoryPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
