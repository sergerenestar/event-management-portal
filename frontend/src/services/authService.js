import apiClient from './apiClient';
import { googleLoginHint, loginRequest, msalInstance } from '../features/auth/msalConfig';

// ── Entra External ID (MSAL) ───────────────────────────────────────────────

/**
 * Opens a Microsoft sign-in popup via MSAL.
 * @returns {Promise<string>} The Entra ID token to exchange with the backend.
 */
export async function loginWithMicrosoft() {
  const response = await msalInstance.loginPopup(loginRequest);
  return response.idToken;
}

/**
 * Opens a Google sign-in popup via Entra External ID.
 * `domain_hint: "google.com"` bypasses the provider picker.
 * @returns {Promise<string>} The Entra ID token to exchange with the backend.
 */
export async function loginWithGoogle() {
  const response = await msalInstance.loginPopup(googleLoginHint);
  return response.idToken;
}

// ── Backend API calls ──────────────────────────────────────────────────────

/**
 * Exchanges an Entra ID token for a portal JWT + HttpOnly refresh cookie.
 * @param {string} idToken  - Raw Entra ID token from MSAL loginPopup.
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
