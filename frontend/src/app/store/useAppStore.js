import { create } from 'zustand';

// Access token is held in-memory only — never written to localStorage or sessionStorage.
// The HttpOnly refresh cookie (ep_refresh) is managed entirely by the browser and backend;
// JS never reads or writes it.

export const useAppStore = create((set) => ({
  accessToken: null,
  admin:       null,
  isLoading:   false,
  authError:   null,

  /** Store a new access token + admin profile after a successful login or refresh. */
  setSession: (token, admin) => set({ accessToken: token, admin, authError: null }),

  /** Wipe the in-memory session (does NOT clear the HttpOnly cookie — call logout API first). */
  clearSession: () => set({ accessToken: null, admin: null }),

  /** Flip the global loading flag (used during auth operations). */
  setLoading: (bool) => set({ isLoading: bool }),

  /** Store an auth error message to surface in the UI. */
  setAuthError: (msg) => set({ authError: msg }),
}));
