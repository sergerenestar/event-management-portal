import { PublicClientApplication } from '@azure/msal-browser';

// ── MSAL application configuration ────────────────────────────────────────

export const msalConfig = {
  auth: {
    clientId:    import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority:   `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID}`,
    redirectUri: import.meta.env.VITE_REDIRECT_URI || 'http://localhost:5173',
  },
  cache: {
    // sessionStorage: cleared on tab close — never persists tokens to localStorage
    cacheLocation:        'sessionStorage',
    storeAuthStateInCookie: false,
  },
};

// ── Login request scopes ───────────────────────────────────────────────────

/** Microsoft sign-in — standard OIDC scopes to get id_token with oid, email, name */
export const loginRequest = {
  scopes: ['openid', 'profile', 'email'],
};

/** Google sign-in — domain_hint skips the Entra provider picker entirely */
export const googleLoginHint = {
  ...loginRequest,
  extraQueryParameters: { domain_hint: 'google.com' },
};

// ── Shared MSAL instance ───────────────────────────────────────────────────
// Single instance exported for use by both MsalProvider and authService.
// MsalProvider handles the required initialize() call on first render.

export const msalInstance = new PublicClientApplication(msalConfig);
