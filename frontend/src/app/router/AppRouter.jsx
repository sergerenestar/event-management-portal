import { Routes, Route, Navigate } from 'react-router-dom';
import AuthGuard from '../../features/auth/AuthGuard';
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

export default function AppRouter() {
  return (
    <Routes>
      {/* Public — redirect to dashboard if already authenticated */}
      <Route path="/login" element={<PublicOnlyRoute><LoginPage /></PublicOnlyRoute>} />

      {/* Root — always go to dashboard (AuthGuard handles the unauth redirect) */}
      <Route path="/" element={<AuthGuard><Navigate to="/dashboard" replace /></AuthGuard>} />

      {/* Protected routes */}
      <Route path="/dashboard"           element={<AuthGuard><DashboardPage /></AuthGuard>} />
      <Route path="/events"              element={<AuthGuard><EventsListPage /></AuthGuard>} />
      <Route path="/events/:id"          element={<AuthGuard><EventDetailPage /></AuthGuard>} />
      <Route path="/communications"      element={<AuthGuard><CampaignsPage /></AuthGuard>} />
      <Route path="/communications/new"  element={<AuthGuard><CampaignComposerPage /></AuthGuard>} />
      <Route path="/social"              element={<AuthGuard><SocialPostsPage /></AuthGuard>} />
      <Route path="/social/generate"     element={<AuthGuard><PostGeneratorPage /></AuthGuard>} />
      <Route path="/content"             element={<AuthGuard><SessionsPage /></AuthGuard>} />
      <Route path="/content/new"         element={<AuthGuard><SessionIngestionPage /></AuthGuard>} />
      <Route path="/content/:id"         element={<AuthGuard><SessionSummaryPage /></AuthGuard>} />
      <Route path="/reports"             element={<AuthGuard><ReportsPage /></AuthGuard>} />
    </Routes>
  );
}
