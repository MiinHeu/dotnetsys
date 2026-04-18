import { Routes, Route, Navigate } from "react-router";
import { useAuthStore } from "./store/authStore";
import { RoleSelect } from "./pages/RoleSelect";
import { AdminLogin } from "./pages/AdminLogin";
import { OwnerLogin } from "./pages/OwnerLogin";
import { OwnerRegister } from "./pages/OwnerRegister";
import { OwnerForgotPassword } from "./pages/OwnerForgotPassword";
import { Layout } from "./components/Layout";
import { Dashboard } from "./pages/Dashboard";
import { Pois } from "./pages/Pois";
import { PoiEditor } from "./pages/PoiEditor";
import { AdminMap } from "./pages/AdminMap";
import { ToursAdmin } from "./pages/ToursAdmin";
import { TourEditor } from "./pages/TourEditor";
import { AnalyticsPage } from "./pages/AnalyticsPage";
import { HistoryPage } from "./pages/HistoryPage";
import { AudioPage } from "./pages/AudioPage";
import { ChangePassword } from "./pages/ChangePassword";
import { UserManagement } from "./pages/UserManagement";
import { DownloadPage } from "./pages/DownloadPage";

export default function App() {
  const token = useAuthStore((s) => s.token);
  const role = useAuthStore((s) => s.role);

  // Not logged in — show auth flow
  if (!token) {
    return (
      <Routes>
        <Route path="/role-select" element={<RoleSelect />} />
        <Route path="/admin-login" element={<AdminLogin />} />
        <Route path="/owner-login" element={<OwnerLogin />} />
        <Route path="/owner-register" element={<OwnerRegister />} />
        <Route path="/owner-forgot-password" element={<OwnerForgotPassword />} />
        <Route path="*" element={<Navigate to="/role-select" />} />
      </Routes>
    );
  }

  // Logged in — show dashboard with role-based routes
  const isAdmin = role === "Admin";

  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/pois" element={<Pois />} />
        <Route path="/pois/:id" element={<PoiEditor />} />
        <Route path="/map" element={<AdminMap />} />
        <Route path="/tours" element={<ToursAdmin />} />
        <Route path="/tours/:id" element={<TourEditor />} />
        <Route path="/audio" element={<AudioPage />} />
        <Route path="/download" element={<DownloadPage />} />
        <Route path="/change-password" element={<ChangePassword />} />

        {/* Admin-only routes */}
        {isAdmin && (
          <>
            <Route path="/analytics" element={<AnalyticsPage />} />
            <Route path="/history" element={<HistoryPage />} />
            <Route path="/users" element={<UserManagement />} />
          </>
        )}

        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </Layout>
  );
}
