import { QueryClient } from "@tanstack/react-query";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,   // 30 giây: cache vẫn fresh
      retry: 2,             // Thử lại 2 lần khi fail
      refetchOnWindowFocus: false,
    },
  },
});
