import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppStore } from '../../app/store/useAppStore';
import { refreshSession, getMe } from '../../services/authService';
import LoadingSpinner from '../../components/feedback/LoadingSpinner';

export default function AuthGuard({ children }) {
  const accessToken  = useAppStore((s) => s.accessToken);
  const setSession   = useAppStore((s) => s.setSession);
  const clearSession = useAppStore((s) => s.clearSession);
  const navigate     = useNavigate();

  // Start checking only when there is no token yet; skip the async path otherwise
  const [checking, setChecking] = useState(!accessToken);

  useEffect(() => {
    if (accessToken) return; // already authenticated — nothing to do

    let cancelled = false;

    async function tryRestore() {
      try {
        const { accessToken: newToken } = await refreshSession();
        const admin = await getMe();
        if (!cancelled) setSession(newToken, admin);
      } catch {
        if (!cancelled) {
          clearSession();
          navigate('/login', { replace: true });
        }
      } finally {
        if (!cancelled) setChecking(false);
      }
    }

    tryRestore();
    return () => { cancelled = true; };
  }, [accessToken, setSession, clearSession, navigate]);

  if (checking) return <LoadingSpinner />;
  return children;
}
