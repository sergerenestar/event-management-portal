import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMsal } from '@azure/msal-react';
import { InteractionStatus } from '@azure/msal-browser';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Divider from '@mui/material/Divider';
import LoadingSpinner from '../../components/feedback/LoadingSpinner';
import ErrorAlert from '../../components/feedback/ErrorAlert';
import {
  loginWithMicrosoft,
  loginWithGoogle,
  handleRedirectResult,
  exchangeEntraToken,
} from '../../services/authService';
import { useAppStore } from '../../app/store/useAppStore';

export default function LoginPage() {
  const navigate   = useNavigate();
  const setSession = useAppStore((s) => s.setSession);

  // inProgress tells us when MSAL has finished initialising and processing any
  // redirect response. We must wait for InteractionStatus.None before calling
  // handleRedirectPromise() so the result is ready.
  const { inProgress } = useMsal();

  const [processing, setProcessing] = useState(false);
  const [error, setError]           = useState(null);

  // ── Handle redirect return ───────────────────────────────────────────────
  useEffect(() => {
    // Wait until MSAL finishes its own initialisation / redirect handling
    if (inProgress !== InteractionStatus.None) return;

    let cancelled = false;

    async function processRedirect() {
      try {
        const result = await handleRedirectResult();
        if (!result || cancelled) return; // no redirect occurred on this page load

        setProcessing(true);
        // Provider was encoded in the `state` param of the original loginRedirect call
        const provider = result.state ?? 'Microsoft';
        const { accessToken, admin } = await exchangeEntraToken(result.idToken, provider);
        if (!cancelled) {
          setSession(accessToken, admin);
          navigate('/dashboard', { replace: true });
        }
      } catch (err) {
        if (!cancelled) {
          setError(err?.response?.data?.message ?? err?.message ?? 'Sign-in failed. Please try again.');
          setProcessing(false);
        }
      }
    }

    processRedirect();
    return () => { cancelled = true; };
  }, [inProgress, navigate, setSession]);

  // ── Button click handlers ────────────────────────────────────────────────
  async function handleLogin(providerFn) {
    setError(null);
    setProcessing(true);
    try {
      // loginRedirect navigates away — setProcessing(false) is never reached on success,
      // but runs if the redirect itself throws (e.g. popup blocked, config error).
      await providerFn();
    } catch (err) {
      setError(err?.message ?? 'Sign-in failed. Please try again.');
      setProcessing(false);
    }
  }

  // Show spinner while MSAL is initialising, processing the redirect, or
  // while we are exchanging the Entra token with our backend.
  const busy = inProgress !== InteractionStatus.None || processing;
  if (busy) return <LoadingSpinner />;

  return (
    <Box
      display="flex"
      justifyContent="center"
      alignItems="center"
      minHeight="100vh"
      bgcolor="grey.100"
    >
      <Paper elevation={3} sx={{ p: 5, width: 380, textAlign: 'center' }}>
        <Typography variant="h5" fontWeight={700} gutterBottom>
          Event Portal
        </Typography>
        <Typography variant="body2" color="text.secondary" mb={3}>
          Admin Access Only
        </Typography>

        {error && (
          <Box mb={2}>
            <ErrorAlert message={error} />
          </Box>
        )}

        <Button
          fullWidth
          variant="contained"
          onClick={() => handleLogin(loginWithMicrosoft)}
          sx={{ mb: 2 }}
        >
          Sign in with Microsoft
        </Button>

        <Divider sx={{ my: 1 }}>or</Divider>

        <Button
          fullWidth
          variant="outlined"
          onClick={() => handleLogin(loginWithGoogle)}
          sx={{ mt: 1 }}
        >
          Sign in with Google
        </Button>
      </Paper>
    </Box>
  );
}
