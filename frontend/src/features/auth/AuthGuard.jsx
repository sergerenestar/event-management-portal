import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAppStore } from '../../app/store/useAppStore';
import LoadingSpinner from '../../components/feedback/LoadingSpinner';

export default function AuthGuard({ children }) {
  const { accessToken, refreshSession } = useAppStore();
  const [checking, setChecking] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    if (accessToken) {
      setChecking(false);
      return;
    }
    refreshSession()
      .catch(() => navigate('/login'))
      .finally(() => setChecking(false));
  }, []);

  if (checking) return <LoadingSpinner />;
  return children;
}
