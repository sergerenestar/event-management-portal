import { useNavigate } from 'react-router-dom';
import { useAppStore } from '../../app/store/useAppStore';
import { logout as authServiceLogout } from '../../services/authService';

/**
 * Provides auth state and a logout action for any component.
 *
 * isAuthenticated — true when an access token is held in-memory
 * admin           — AdminUserDto from the store, or null
 * logout()        — revokes server-side session, clears store, navigates to /login
 */
export function useAuth() {
  const navigate      = useNavigate();
  const accessToken   = useAppStore((s) => s.accessToken);
  const admin         = useAppStore((s) => s.admin);
  const clearSession  = useAppStore((s) => s.clearSession);

  async function logout() {
    try {
      await authServiceLogout(); // revokes refresh cookie + MSAL sign-out
    } finally {
      clearSession();
      navigate('/login');
    }
  }

  return {
    isAuthenticated: Boolean(accessToken),
    admin,
    logout,
  };
}
