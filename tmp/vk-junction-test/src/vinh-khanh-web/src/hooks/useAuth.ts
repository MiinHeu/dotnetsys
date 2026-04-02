import { useAuthStore } from "../store/authStore";

export function useAuth() {
  const token = useAuthStore((s) => s.token);
  const role = useAuthStore((s) => s.role);
  const authHeader = token ? { Authorization: `Bearer ${token}` } : {};
  return { token, role, authHeader };
}
