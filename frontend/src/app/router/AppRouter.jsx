import { Routes, Route, Navigate } from 'react-router-dom';
import AuthGuard from '../../features/auth/AuthGuard';
import AppShell from '../../components/layout/AppShell';
import LoginPage from '../../features/auth/LoginPage';
import DashboardPage from '../../features/dashboard/DashboardPage';
import EventsListPage from '../../features/events/EventsListPage';
import EventDetailPage from '../../features/events/EventDetailPage';
import CampaignsPage from '../../features/communications/CampaignsPage';
import CampaignComposerPage from '../../features/communications/CampaignComposerPage';
import SocialPostsPage from '../../features/social/SocialPostsPage';
import PostGeneratorPage from '../../features/social/PostGeneratorPage';
import SessionsPage from '../../features/content/SessionsPage';
import SessionIngestionPage from '../../features/content/SessionIngestionPage';
import SessionSummaryPage from '../../features/content/SessionSummaryPage';
import ReportsPage from '../../features/reports/ReportsPage';
import { useAppStore } from '../store/useAppStore';

/** Redirects to /dashboard if an access token is already in-memory. */
function PublicOnlyRoute({ children }) {
  const accessToken = useAppStore((s) => s.accessToken);
  return accessToken ? <Navigate to="/dashboard" replace /> : children;
}

/** Authenticates and renders the page inside the full app shell. */
function ProtectedLayout({ children }) {
  return (
    <AuthGuard>
      <AppShell>{children}</AppShell>
    </AuthGuard>
  );
}

export default function AppRouter() {
  return (
    <Routes>
      {/* Public — redirect to dashboard if already authenticated */}
      <Route path="/login" element={<PublicOnlyRoute><LoginPage /></PublicOnlyRoute>} />

      {/* Root — always go to dashboard */}
      <Route path="/" element={<ProtectedLayout><Navigate to="/dashboard" replace /></ProtectedLayout>} />

      {/* Protected routes — all wrapped in AuthGuard + AppShell */}
      <Route path="/dashboard"          element={<ProtectedLayout><DashboardPage /></ProtectedLayout>} />
      <Route path="/events"             element={<ProtectedLayout><EventsListPage /></ProtectedLayout>} />
      <Route path="/events/:id"         element={<ProtectedLayout><EventDetailPage /></ProtectedLayout>} />
      <Route path="/communications"     element={<ProtectedLayout><CampaignsPage /></ProtectedLayout>} />
      <Route path="/communications/new" element={<ProtectedLayout><CampaignComposerPage /></ProtectedLayout>} />
      <Route path="/social"             element={<ProtectedLayout><SocialPostsPage /></ProtectedLayout>} />
      <Route path="/social/generate"    element={<ProtectedLayout><PostGeneratorPage /></ProtectedLayout>} />
      <Route path="/content"            element={<ProtectedLayout><SessionsPage /></ProtectedLayout>} />
      <Route path="/content/new"        element={<ProtectedLayout><SessionIngestionPage /></ProtectedLayout>} />
      <Route path="/content/:id"        element={<ProtectedLayout><SessionSummaryPage /></ProtectedLayout>} />
      <Route path="/reports"            element={<ProtectedLayout><ReportsPage /></ProtectedLayout>} />
    </Routes>
  );
}
