import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Divider from '@mui/material/Divider';
import Grid from '@mui/material/Grid';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Typography from '@mui/material/Typography';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import CalendarTodayIcon from '@mui/icons-material/CalendarToday';
import EventCard from './EventCard';
import { getEvents } from '../../services/eventService';

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });
}

function StatCard({ label, value, sub, icon }) {
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.5, borderRadius: 2, display: 'flex', alignItems: 'flex-start', gap: 2 }}
    >
      {icon && (
        <Box sx={{ p: 1, bgcolor: 'primary.50', borderRadius: 1.5, color: 'primary.main', display: 'flex' }}>
          {icon}
        </Box>
      )}
      <Box>
        <Typography variant="body2" color="text.secondary" gutterBottom>{label}</Typography>
        <Typography variant="h5" fontWeight={700} lineHeight={1}>{value}</Typography>
        {sub && <Typography variant="caption" color="text.secondary" mt={0.5} display="block">{sub}</Typography>}
      </Box>
    </Paper>
  );
}

function SkeletonStatRow() {
  return (
    <Grid container spacing={2} sx={{ mb: 4 }}>
      {[0, 1, 2].map((i) => (
        <Grid item xs={12} sm={4} key={i}>
          <Skeleton variant="rectangular" height={88} sx={{ borderRadius: 2 }} />
        </Grid>
      ))}
    </Grid>
  );
}

function SkeletonGrid() {
  return (
    <Grid container spacing={2}>
      {Array.from({ length: 6 }).map((_, i) => (
        <Grid item xs={12} sm={6} md={4} key={i}>
          <Skeleton variant="rectangular" height={280} sx={{ borderRadius: 2 }} />
        </Grid>
      ))}
    </Grid>
  );
}

export default function DashboardPage() {
  const navigate = useNavigate();
  const [events, setEvents]   = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState(null);

  useEffect(() => {
    getEvents()
      .then(setEvents)
      .catch(() => setError('Failed to load dashboard data.'))
      .finally(() => setLoading(false));
  }, []);

  // ── Derived stats ──────────────────────────────────────────────────────
  const totalRegistrations = events.reduce((sum, e) => sum + (e.totalRegistrations ?? 0), 0);
  const now = new Date();
  const nextEvent = events
    .filter((e) => new Date(e.startDate) > now && e.status !== 'cancelled')
    .sort((a, b) => new Date(a.startDate) - new Date(b.startDate))[0] ?? null;
  const displayedEvents = events.slice(0, 6);

  // ── Render ─────────────────────────────────────────────────────────────
  return (
    <Box sx={{ p: 3 }}>
      {/* Page heading */}
      <Box mb={3}>
        <Typography variant="h5" fontWeight={600}>Dashboard</Typography>
        <Typography variant="body2" color="text.secondary" mt={0.5}>
          Overview of your event portfolio
        </Typography>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

      {/* Stat row */}
      {loading ? <SkeletonStatRow /> : (
        <Grid container spacing={2} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={4}>
            <StatCard
              label="Total Events"
              value={events.length.toLocaleString()}
              sub={`${events.filter((e) => e.status === 'live').length} live`}
            />
          </Grid>
          <Grid item xs={12} sm={4}>
            <StatCard
              label="Total Registrations"
              value={totalRegistrations.toLocaleString()}
              sub="Across all events"
            />
          </Grid>
          <Grid item xs={12} sm={4}>
            <StatCard
              icon={<CalendarTodayIcon fontSize="small" />}
              label="Next Upcoming Event"
              value={nextEvent ? nextEvent.name : '—'}
              sub={nextEvent ? formatDate(nextEvent.startDate) : 'No upcoming events'}
            />
          </Grid>
        </Grid>
      )}

      <Divider sx={{ mb: 3 }} />

      {/* Event grid header */}
      <Box display="flex" alignItems="center" justifyContent="space-between" mb={2}>
        <Typography variant="h6" fontWeight={600}>Recent Events</Typography>
        {!loading && events.length > 6 && (
          <Button
            endIcon={<ArrowForwardIcon />}
            onClick={() => navigate('/events')}
            size="small"
          >
            View all {events.length} events
          </Button>
        )}
      </Box>

      {/* Event grid */}
      {loading ? <SkeletonGrid /> : events.length === 0 ? (
        <Paper
          variant="outlined"
          sx={{ p: 6, borderRadius: 2, textAlign: 'center', bgcolor: 'grey.50' }}
        >
          <Typography color="text.secondary" gutterBottom>No events yet.</Typography>
          <Button variant="contained" onClick={() => navigate('/events')} size="small">
            Go to Events to sync from Eventbrite
          </Button>
        </Paper>
      ) : (
        <>
          <Grid container spacing={2}>
            {displayedEvents.map((event) => (
              <Grid item xs={12} sm={6} md={4} key={event.id}>
                <EventCard event={event} />
              </Grid>
            ))}
          </Grid>
          {events.length > 6 && (
            <Box textAlign="center" mt={3}>
              <Button
                variant="outlined"
                endIcon={<ArrowForwardIcon />}
                onClick={() => navigate('/events')}
              >
                View all {events.length} events
              </Button>
            </Box>
          )}
        </>
      )}
    </Box>
  );
}
