# 07 — Frontend Specification
# Event Management Portal — React Application

> Version: 1.0
> Status: Approved
> Audience: Frontend contributors, AI coding agents

---

## 1. Frontend Design Principles

- Feature-based folder structure — each domain owns its own components, hooks, and service calls
- No feature may call `fetch` or `axios` directly — always via a service module in `src/services/`
- Shared UI components live in `src/components/` — features do not duplicate them
- Auth state is global — managed via Zustand store
- Every route except `/login` requires the auth guard to pass
- Single UI component library: **Material UI (MUI v5)** — do not mix with other component libraries
- Charts: **Recharts** — consistent across all dashboard views
- HTTP client: **Axios** — centralized in `src/services/apiClient.js`
- Routing: **React Router v6**

---

## 2. Folder Structure

```
src/
├── app/
│   ├── router/
│   │   └── AppRouter.jsx           # All route definitions
│   ├── providers/
│   │   └── AppProviders.jsx        # MUI ThemeProvider, Router, Zustand store init
│   └── store/
│       └── useAppStore.js          # Zustand store: auth state, global UI state
│
├── features/
│   ├── auth/
│   │   ├── LoginPage.jsx
│   │   ├── useAuth.js              # Hook wrapping authService and store actions
│   │   └── AuthGuard.jsx           # Route guard component
│   ├── dashboard/
│   │   ├── DashboardPage.jsx
│   │   └── components/
│   │       ├── StatCard.jsx
│   │       └── RecentActivityFeed.jsx
│   ├── events/
│   │   ├── EventsListPage.jsx
│   │   ├── EventDetailPage.jsx
│   │   └── components/
│   │       ├── EventCard.jsx
│   │       └── SyncStatusBadge.jsx
│   ├── registrations/
│   │   ├── RegistrationSummaryPanel.jsx
│   │   ├── DailyTrendChart.jsx
│   │   └── TicketTypeSummaryTable.jsx
│   ├── communications/
│   │   ├── CampaignsPage.jsx
│   │   ├── CampaignComposerPage.jsx
│   │   └── components/
│   │       ├── CampaignHistoryTable.jsx
│   │       └── SegmentSelector.jsx
│   ├── social/
│   │   ├── SocialPostsPage.jsx
│   │   ├── PostGeneratorPage.jsx
│   │   └── components/
│   │       ├── PostDraftCard.jsx
│   │       ├── PostApprovalActions.jsx
│   │       └── PublishHistoryTable.jsx
│   ├── content/
│   │   ├── SessionsPage.jsx
│   │   ├── SessionIngestionPage.jsx
│   │   ├── SessionSummaryPage.jsx
│   │   └── components/
│   │       ├── SessionStatusBadge.jsx
│   │       ├── QuoteCard.jsx
│   │       └── SummaryViewer.jsx
│   └── reports/
│       ├── ReportsPage.jsx
│       └── components/
│           └── ReportHistoryTable.jsx
│
├── components/
│   ├── layout/
│   │   ├── AppShell.jsx            # Sidebar + top bar layout wrapper
│   │   ├── Sidebar.jsx             # Navigation drawer
│   │   └── TopBar.jsx              # App bar with admin name + sign out
│   ├── charts/
│   │   └── DailyTrendLineChart.jsx # Reusable Recharts line chart
│   ├── forms/
│   │   ├── FormTextField.jsx
│   │   └── FormSelect.jsx
│   ├── tables/
│   │   └── DataTable.jsx           # Reusable MUI DataGrid wrapper
│   └── feedback/
│       ├── LoadingSpinner.jsx
│       ├── ErrorAlert.jsx
│       ├── SuccessBanner.jsx
│       └── EmptyState.jsx
│
├── services/
│   ├── apiClient.js                # Axios instance, auth header injection, 401 handler
│   ├── authService.js              # login, logout, refresh, getMe
│   ├── eventService.js             # getEvents, getEvent, syncEvents
│   ├── registrationService.js      # getSummary, getDailyTrends, syncRegistrations
│   ├── campaignService.js          # getSegments, createCampaign, sendCampaign, getCampaigns
│   ├── socialService.js            # generatePost, getPosts, approvePost, publishPost
│   ├── sessionService.js           # createSession, getSessions, generateSummary, approveSummary
│   └── reportService.js            # generateReport, getReports, getDownloadUrl
│
└── utils/
    ├── dateUtils.js                # Format UTC dates, relative time helpers
    ├── statusColors.js             # Map status strings to MUI color tokens
    └── validators.js               # Client-side validation helpers
```

---

## 3. Routing

All routes are defined in `src/app/router/AppRouter.jsx`.

| Path | Component | Guard |
|---|---|---|
| `/` | Redirect to `/dashboard` | Auth required |
| `/login` | `LoginPage` | Public |
| `/dashboard` | `DashboardPage` | Auth required |
| `/events` | `EventsListPage` | Auth required |
| `/events/:id` | `EventDetailPage` | Auth required |
| `/communications` | `CampaignsPage` | Auth required |
| `/communications/new` | `CampaignComposerPage` | Auth required |
| `/social` | `SocialPostsPage` | Auth required |
| `/social/generate` | `PostGeneratorPage` | Auth required |
| `/content` | `SessionsPage` | Auth required |
| `/content/new` | `SessionIngestionPage` | Auth required |
| `/content/:id` | `SessionSummaryPage` | Auth required |
| `/reports` | `ReportsPage` | Auth required |
| `/settings` | `SettingsPage` | Auth required |

---

## 4. Auth Guard

`AuthGuard.jsx` wraps all protected routes.

**Behavior:**
1. On mount, check Zustand store for a valid `accessToken`
2. If token exists and not expired: render children
3. If token missing but refresh cookie may exist: call `authService.refresh()`
   - If refresh succeeds: store new token, render children
   - If refresh fails: redirect to `/login`
4. While refresh is in flight: show `LoadingSpinner`

```jsx
// AuthGuard.jsx
export function AuthGuard({ children }) {
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
```

---

## 5. Zustand Store

`src/app/store/useAppStore.js` manages global auth state.

**State shape:**
```js
{
  accessToken: null,         // string | null — in memory only
  admin: null,               // { id, email, displayName } | null
  setSession: (token, admin) => void,
  clearSession: () => void,
  refreshSession: async () => void,  // calls authService.refresh()
}
```

**Rules:**
- `accessToken` is stored in memory only — never in `localStorage` or `sessionStorage`
- Refresh token is an `HttpOnly` cookie managed by the browser — store has no access to it
- `clearSession()` is called on logout and on any 401 that fails to refresh

---

## 6. API Client

`src/services/apiClient.js` is the single Axios instance for all API calls.

**Configuration:**
- `baseURL` from `VITE_API_BASE_URL` environment variable
- Request interceptor: attaches `Authorization: Bearer <accessToken>` from Zustand store
- Response interceptor: on 401, attempts one silent refresh via `authService.refresh()`
  - If refresh succeeds: retry the original request
  - If refresh fails: call `clearSession()` and redirect to `/login`

```js
// src/services/apiClient.js
import axios from 'axios';
import { useAppStore } from '../app/store/useAppStore';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,  // required for HttpOnly refresh cookie
});

apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAppStore.getState();
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// 401 interceptor with refresh retry (full implementation in source)

export default apiClient;
```

---

## 7. Key Pages and Components

### Login Page
- Portal logo and name
- "Sign In" button triggers MSAL popup or redirect
- Supports Microsoft account and Google (both federated via Entra)
- Displays error message if login or token exchange fails
- No self-registration UI

### App Shell (Layout)
- Persistent sidebar on desktop, collapsible drawer on mobile
- Top bar: portal name, current page title, admin display name, Sign Out
- Main content area renders the active route

### Sidebar Navigation
```
Dashboard
Events
  └─ Registrations (nested under Event Detail)
Communications
Social Posts
Content (Sessions)
Reports
Settings
```

### Event Detail Page
- Event metadata (name, dates, venue, status)
- Registration Summary Panel: total by ticket type
- Daily Trend Chart: Recharts line chart by ticket type
- Sync status and last synced timestamp
- Manual sync trigger button

### Campaign Composer Page
- Event selector (dropdown)
- Segment selector (fetched from `campaignService.getSegments()`)
- Message body textarea with character count (max 1600)
- Preview section
- Save Draft / Send Campaign buttons with confirmation dialog on Send

### Post Generator Page
- Event and optional session selector
- Platform checkboxes: Facebook, Instagram, Both
- Post type selector: Promotion, Recap, Quote
- Context notes textarea
- "Generate Draft" button
- Draft preview with editable caption and hashtag fields
- Approve and Publish actions (conditional on status)

### Session Summary Page
- Session metadata (title, speaker, YouTube URL, status badges)
- Transcript status indicator
- Summary viewer: markdown rendered with MUI Typography
- Key Takeaways list
- Themes chips
- Action Points list
- Quotes table with per-quote approval toggle
- Approve Full Summary button
- Generate Social Snippet button (links to Post Generator with session pre-filled)

---

## 8. Status Color Conventions

All status strings are mapped to MUI color tokens via `src/utils/statusColors.js`:

| Status | MUI Color |
|---|---|
| `draft` | `default` (grey) |
| `reviewed` | `info` (blue) |
| `approved` | `success` (green) |
| `published` | `primary` |
| `rejected` | `error` |
| `failed` | `error` |
| `pending` | `warning` |
| `processing` | `info` |
| `complete` | `success` |
| `sent` | `success` |

---

## 9. Environment Variables

All environment config is in `.env` files with `VITE_` prefix:

```
VITE_API_BASE_URL=https://api.eventportal.dev
VITE_MSAL_CLIENT_ID=<entra-client-id>
VITE_MSAL_AUTHORITY=https://login.microsoftonline.com/<tenant-id>
VITE_MSAL_REDIRECT_URI=https://eventportal.dev/login
```

- No secrets in frontend env vars — these are all public client-side config values
- Never log `VITE_MSAL_CLIENT_ID` or token values in console output

---

## 10. Component Rules

| Rule | Detail |
|---|---|
| No raw `fetch` or `axios` in features | All calls go through `src/services/*.js` |
| No direct MUI theme override inside features | Use `sx` prop or `styled()` with theme tokens only |
| Error states always handled | Every service call has loading, error, and empty state |
| Forms use controlled components | No uncontrolled inputs — all values in React state |
| No `localStorage` or `sessionStorage` | Auth token in memory, refresh via cookie only |
| No hardcoded API paths in components | Paths defined in service files only |
