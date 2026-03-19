import apiClient from './apiClient';
import { googleLoginHint, loginRequest, msalInstance } from '../features/auth/msalConfig';

// ── Entra External ID (MSAL) ───────────────────────────────────────────────

/**
 * Initiates a Microsoft sign-in redirect via MSAL.
 * The provider name is encoded in `state` so LoginPage can recover it on return.
 * Returns Promise<void> — the page navigates away immediately.
 */
export async function loginWithMicrosoft() {
  await msalInstance.loginRedirect({ ...loginRequest, state: 'Microsoft' });
}

/**
 * Initiates a Google sign-in redirect via Entra External ID.
 * `domain_hint: "google.com"` bypasses the provider picker.
 * Returns Promise<void> — the page navigates away immediately.
 */
export async function loginWithGoogle() {
  await msalInstance.loginRedirect({ ...googleLoginHint, state: 'Google' });
}

/**
 * Processes the redirect response after MSAL returns to the app.
 * Safe to call alongside MsalProvider — MSAL caches the promise so duplicate
 * calls return the same AuthenticationResult (or null if no redirect occurred).
 * @returns {Promise<import('@azure/msal-browser').AuthenticationResult | null>}
 */
export async function handleRedirectResult() {
  return msalInstance.handleRedirectPromise();
}

// ── Backend API calls ──────────────────────────────────────────────────────

/**
 * Exchanges an Entra ID token for a portal JWT + HttpOnly refresh cookie.
 * @param {string} idToken  - Raw Entra ID token from MSAL redirect result.
 * @param {string} provider - "Microsoft" | "Google"
 * @returns {Promise<{accessToken: string, expiresIn: number, admin: object}>}
 */
export async function exchangeEntraToken(idToken, provider) {
  const { data } = await apiClient.post('/api/v1/auth/login', {
    entraIdToken: idToken,
    provider,
  });
  return data;
}

/**
 * Silently refreshes the session using the HttpOnly refresh cookie.
 * The cookie is sent automatically by the browser; JS never reads it.
 * @returns {Promise<{accessToken: string, expiresIn: number}>}
 */
export async function refreshSession() {
  const { data } = await apiClient.post('/api/v1/auth/refresh');
  return data;
}

/**
 * Returns the authenticated admin's profile from the backend.
 * @returns {Promise<{id: number, email: string, displayName: string, isActive: boolean}>}
 */
export async function getMe() {
  const { data } = await apiClient.get('/api/v1/auth/me');
  return data;
}

/**
 * Logs out: revokes the server-side refresh token, clears the HttpOnly cookie,
 * and signs out of MSAL. Store clearing and navigation are the caller's responsibility
 * (see useAuth.js).
 */
export async function logout() {
  try {
    await apiClient.post('/api/v1/auth/logout');
  } catch {
    // Backend logout best-effort — proceed with MSAL sign-out regardless
  }
  await msalInstance.logoutPopup({ postLogoutRedirectUri: '/' });
}
