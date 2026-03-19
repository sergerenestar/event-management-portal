import { useCallback, useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Divider from '@mui/material/Divider';
import Grid from '@mui/material/Grid';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Snackbar from '@mui/material/Snackbar';
import Typography from '@mui/material/Typography';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import CalendarTodayIcon from '@mui/icons-material/CalendarToday';
import LocationOnIcon from '@mui/icons-material/LocationOn';
import SyncIcon from '@mui/icons-material/Sync';
import EventStatusChip from './EventStatusChip';
import TicketTypesTable from './TicketTypesTable';
import DailyTrendChart from './DailyTrendChart';
import { getEventById } from '../../services/eventService';
import { getSummary, getByTicketType, getDailyTrends, syncRegistrations } from '../../services/registrationService';

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(undefined, {
    weekday: 'short', year: 'numeric', month: 'short', day: 'numeric',
  });
}

function StatCard({ label, value, sub }) {
  return (
    <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 2, height: '100%' }}>
      <Typography variant="body2" color="text.secondary" gutterBottom>{label}</Typography>
      <Typography variant="h4" fontWeight={700} lineHeight={1}>{value}</Typography>
      {sub && <Typography variant="caption" color="text.secondary" mt={0.5}>{sub}</Typography>}
    </Paper>
  );
}

function PageSkeleton() {
  return (
    <Box sx={{ p: 3 }}>
      <Skeleton width={120} height={36} sx={{ mb: 2 }} />
      <Skeleton width="50%" height={40} sx={{ mb: 1 }} />
      <Skeleton width="30%" height={24} sx={{ mb: 3 }} />
      <Grid container spacing={2} sx={{ mb: 4 }}>
        {[0, 1, 2].map((i) => (
          <Grid item xs={12} sm={4} key={i}>
            <Skeleton variant="rectangular" height={96} sx={{ borderRadius: 2 }} />
          </Grid>
        ))}
      </Grid>
      <Skeleton variant="rectangular" height={200} sx={{ borderRadius: 2, mb: 3 }} />
      <Skeleton variant="rectangular" height={320} sx={{ borderRadius: 2 }} />
    </Box>
  );
}

export default function EventDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [event, setEvent]           = useState(null);
  const [summary, setSummary]       = useState(null);
  const [ticketTypes, setTicketTypes] = useState([]);
  const [trends, setTrends]         = useState([]);
  const [loading, setLoading]       = useState(true);
  const [error, setError]           = useState(null);
  const [syncing, setSyncing]       = useState(false);
  const [snackbar, setSnackbar]     = useState({ open: false, message: '', severity: 'success' });

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [ev, sum, tts, trds] = await Promise.all([
        getEventById(id),
        getSummary(id),
        getByTicketType(id),
        getDailyTrends(id),
      ]);
      setEvent(ev);
      setSummary(sum);
      setTicketTypes(tts);
      setTrends(trds);
    } catch {
      setError('Failed to load event data. Please try again.');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  async function handleSyncRegistrations() {
    setSyncing(true);
    try {
      await syncRegistrations(id);
      setSnackbar({ open: true, message: 'Registration sync started — data will refresh shortly.', severity: 'success' });
      setTimeout(load, 3000);
    } catch {
      setSnackbar({ open: true, message: 'Sync failed. Please try again.', severity: 'error' });
    } finally {
      setSyncing(false);
    }
  }

  if (loading) return <PageSkeleton />;

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error" action={
          <Button size="small" onClick={load}>Retry</Button>
        }>{error}</Alert>
      </Box>
    );
  }

  return (
    <Box sx={{ p: 3, maxWidth: 1100, mx: 'auto' }}>
      {/* ── Back + Sync button ── */}
      <Box display="flex" alignItems="center" justifyContent="space-between" mb={3}>
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={() => navigate('/events')}
          size="small"
          color="inherit"
        >
          All Events
        </Button>
        <Button
          variant="outlined"
          startIcon={<SyncIcon />}
          onClick={handleSyncRegistrations}
          disabled={syncing}
          size="small"
        >
          {syncing ? 'Syncing…' : 'Sync Registrations'}
        </Button>
      </Box>

      {/* ── Event header ── */}
      <Box display="flex" gap={2} alignItems="flex-start" mb={1}>
        {event.thumbnailUrl && (
          <Box
            component="img"
            src={event.thumbnailUrl}
            alt={event.name}
            sx={{ width: 80, height: 80, objectFit: 'cover', borderRadius: 2, flexShrink: 0 }}
          />
        )}
        <Box>
          <Box display="flex" alignItems="center" gap={1} mb={0.5}>
            <Typography variant="h5" fontWeight={700}>{event.name}</Typography>
            <EventStatusChip status={event.status} />
          </Box>
          <Box display="flex" gap={2} flexWrap="wrap">
            <Box display="flex" alignItems="center" gap={0.5}>
              <CalendarTodayIcon fontSize="small" sx={{ color: 'text.secondary' }} />
              <Typography variant="body2" color="text.secondary">
                {formatDate(event.startDate)}
                {event.endDate && event.endDate !== event.startDate && ` – ${formatDate(event.endDate)}`}
              </Typography>
            </Box>
            {event.venue && (
              <Box display="flex" alignItems="center" gap={0.5}>
                <LocationOnIcon fontSize="small" sx={{ color: 'text.secondary' }} />
                <Typography variant="body2" color="text.secondary">{event.venue}</Typography>
              </Box>
            )}
          </Box>
        </Box>
      </Box>

      <Divider sx={{ my: 3 }} />

      {/* ── Stat cards ── */}
      <Grid container spacing={2} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={4}>
          <StatCard
            label="Total Registrations"
            value={(summary?.totalRegistrations ?? 0).toLocaleString()}
          />
        </Grid>
        <Grid item xs={12} sm={4}>
          <StatCard
            label="Total Capacity"
            value={(summary?.totalCapacity ?? 0).toLocaleString()}
          />
        </Grid>
        <Grid item xs={12} sm={4}>
          <StatCard
            label="Fill Rate"
            value={`${(summary?.fillRate ?? 0).toFixed(1)}%`}
            sub={summary?.lastSyncAt ? `Last synced ${formatDate(summary.lastSyncAt)}` : null}
          />
        </Grid>
      </Grid>

      {/* ── Ticket types table ── */}
      <Typography variant="h6" fontWeight={600} mb={1.5}>Ticket Types</Typography>
      <Box mb={4}>
        <TicketTypesTable ticketTypes={ticketTypes} />
      </Box>

      {/* ── Daily trend chart ── */}
      <Typography variant="h6" fontWeight={600} mb={1.5}>Daily Registration Trend</Typography>
      <Paper variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
        <DailyTrendChart snapshots={trends} />
      </Paper>

      {/* ── Snackbar ── */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={4000}
        onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity={snackbar.severity}
          onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
          variant="filled"
          sx={{ width: '100%' }}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
}
