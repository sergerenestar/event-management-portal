import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Divider from '@mui/material/Divider';
import LoadingSpinner from '../../components/feedback/LoadingSpinner';
import ErrorAlert from '../../components/feedback/ErrorAlert';
import { loginWithMicrosoft, loginWithGoogle, exchangeEntraToken } from '../../services/authService';
import { useAppStore } from '../../app/store/useAppStore';

export default function LoginPage() {
  const navigate    = useNavigate();
  const setSession  = useAppStore((s) => s.setSession);
  const [loading, setLoading] = useState(false);
  const [error, setError]     = useState(null);

  async function handleLogin(providerFn, providerName) {
    setError(null);
    setLoading(true);
    try {
      const idToken = await providerFn();
      const { accessToken, expiresIn: _expiresIn, admin } = await exchangeEntraToken(idToken, providerName);
      setSession(accessToken, admin);
      navigate('/dashboard');
    } catch (err) {
      setError(err?.response?.data?.message ?? err?.message ?? 'Sign-in failed. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  if (loading) return <LoadingSpinner />;

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
          onClick={() => handleLogin(loginWithMicrosoft, 'Microsoft')}
          sx={{ mb: 2 }}
        >
          Sign in with Microsoft
        </Button>

        <Divider sx={{ my: 1 }}>or</Divider>

        <Button
          fullWidth
          variant="outlined"
          onClick={() => handleLogin(loginWithGoogle, 'Google')}
          sx={{ mt: 1 }}
        >
          Sign in with Google
        </Button>
      </Paper>
    </Box>
  );
}
