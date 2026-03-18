import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material';
import { MsalProvider } from '@azure/msal-react';
import { msalInstance } from '../../features/auth/msalConfig';

const theme = createTheme();

export default function AppProviders({ children }) {
  return (
    <MsalProvider instance={msalInstance}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          {children}
        </BrowserRouter>
      </ThemeProvider>
    </MsalProvider>
  );
}
