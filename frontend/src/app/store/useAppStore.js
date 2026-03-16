import { create } from 'zustand';
import { authService } from '../../services/authService';

export const useAppStore = create((set) => ({
  accessToken: null,
  admin: null,
  setSession: (token, admin) => set({ accessToken: token, admin }),
  clearSession: () => set({ accessToken: null, admin: null }),
  refreshSession: async () => {
    const result = await authService.refresh();
    set({ accessToken: result.accessToken, admin: result.admin });
  },
}));
