import { create } from "zustand";
import { persist } from "zustand/middleware";

interface AuthState {
  token: string | null;
  role: string | null;           // "Admin" | "Owner"
  selectedPortal: string | null; // "admin" | "owner" — chosen at role-select screen
  setAuth: (token: string, role: string) => void;
  setPortal: (portal: "admin" | "owner") => void;
  logout: () => void;
  clear: () => void; // alias cho logout — Layout.tsx hiện tại dùng clear()
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      role: null,
      selectedPortal: null,
      setAuth: (token, role) => set({ token, role }),
      setPortal: (portal) => set({ selectedPortal: portal }),
      logout: () => set({ token: null, role: null, selectedPortal: null }),
      clear: () => set({ token: null, role: null, selectedPortal: null }),
    }),
    { name: "auth-storage" }
  )
);
