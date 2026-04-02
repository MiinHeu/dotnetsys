import { Routes, Route, Navigate } from "react-router";
import { useAuthStore } from "./store/authStore";
import { Login } from "./pages/Login";
import { Layout } from "./components/Layout";
import { Dashboard } from "./pages/Dashboard";
import { Pois } from "./pages/Pois";
import { PoiEditor } from "./pages/PoiEditor";
import { AdminMap } from "./pages/AdminMap";
import { ToursAdmin } from "./pages/ToursAdmin";
import { TourEditor } from "./pages/TourEditor";
import { Translations } from "./pages/Translations";
import { AnalyticsPage } from "./pages/AnalyticsPage";
import { HistoryPage } from "./pages/HistoryPage";
import { AudioPage } from "./pages/AudioPage";

export default function App() {
  const token = useAuthStore((s) => s.token);

  if (!token) return <Login />;

  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/pois" element={<Pois />} />
        <Route path="/pois/:id" element={<PoiEditor />} />
        <Route path="/map" element={<AdminMap />} />
        <Route path="/tours" element={<ToursAdmin />} />
        <Route path="/tours/:id" element={<TourEditor />} />
        <Route path="/translations" element={<Translations />} />
        <Route path="/analytics" element={<AnalyticsPage />} />
        <Route path="/history" element={<HistoryPage />} />
        <Route path="/audio" element={<AudioPage />} />
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </Layout>
  );
}
