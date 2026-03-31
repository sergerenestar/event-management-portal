import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Paper from '@mui/material/Paper';
import Skeleton from '@mui/material/Skeleton';
import Snackbar from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import SyncIcon from '@mui/icons-material/Sync';
import EmptyState from '../../components/feedback/EmptyState';
import EventStatusChip from './EventStatusChip';
import { getEvents, syncEvents } from '../../services/eventService';

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

function SkeletonRows() {
  return Array.from({ length: 5 }).map((_, i) => (
    <TableRow key={i}>
      <TableCell><Skeleton variant="rectangular" width={48} height={48} sx={{ borderRadius: 1 }} /></TableCell>
      <TableCell><Skeleton width="60%" /></TableCell>
      <TableCell><Skeleton width="80px" /></TableCell>
      <TableCell><Skeleton width="100px" /></TableCell>
      <TableCell><Skeleton width="60px" /></TableCell>
      <TableCell><Skeleton width="40px" /></TableCell>
    </TableRow>
  ));
}

export default function EventsListPage() {
  const navigate = useNavigate();
  const [events, setEvents]     = useState([]);
  const [loading, setLoading]   = useState(true);
  const [syncing, setSyncing]   = useState(false);
  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });

  useEffect(() => {
    fetchEvents();
  }, []);

  async function fetchEvents() {
    setLoading(true);
    try {
      const data = await getEvents();
      setEvents(data);
    } catch {
      setSnackbar({ open: true, message: 'Failed to load events.', severity: 'error' });
    } finally {
      setLoading(false);
    }
  }

  async function handleSync() {
    setSyncing(true);
    try {
      await syncEvents();
      setSnackbar({ open: true, message: 'Sync started — refreshing in a moment…', severity: 'success' });
      // Re-fetch after delay to give the Hangfire job time to complete
      setTimeout(fetchEvents, 8000);
    } catch {
      setSnackbar({ open: true, message: 'Sync failed. Please try again.', severity: 'error' });
    } finally {
      setSyncing(false);
    }
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* ── Header ── */}
      <Box display="flex" alignItems="center" justifyContent="space-between" mb={3}>
        <Box>
          <Typography variant="h5" fontWeight={600}>Events</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            All events synced from Eventbrite
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<SyncIcon />}
          onClick={handleSync}
          disabled={syncing || loading}
        >
          {syncing ? 'Syncing…' : 'Sync from Eventbrite'}
        </Button>
      </Box>

      {/* ── Table ── */}
      <TableContainer component={Paper} variant="outlined">
        <Table>
          <TableHead>
            <TableRow sx={{ bgcolor: 'grey.50' }}>
              <TableCell sx={{ width: 64 }} />
              <TableCell><Typography variant="subtitle2">Event</Typography></TableCell>
              <TableCell><Typography variant="subtitle2">Date</Typography></TableCell>
              <TableCell><Typography variant="subtitle2">Venue</Typography></TableCell>
              <TableCell><Typography variant="subtitle2">Status</Typography></TableCell>
              <TableCell align="right"><Typography variant="subtitle2">Registrations</Typography></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading ? (
              <SkeletonRows />
            ) : events.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} sx={{ py: 6 }}>
                  <EmptyState message="No events yet. Click 'Sync from Eventbrite' to import your events." />
                </TableCell>
              </TableRow>
            ) : (
              events.map((event) => (
                <TableRow
                  key={event.id}
                  hover
                  onClick={() => navigate(`/events/${event.id}/analytics`)}
                  sx={{ cursor: 'pointer' }}
                >
                  <TableCell>
                    {event.thumbnailUrl ? (
                      <Box
                        component="img"
                        src={event.thumbnailUrl}
                        alt={event.name}
                        sx={{ width: 48, height: 48, objectFit: 'cover', borderRadius: 1, display: 'block' }}
                      />
                    ) : (
                      <Box
                        sx={{
                          width: 48, height: 48, borderRadius: 1,
                          bgcolor: 'grey.200', display: 'flex',
                          alignItems: 'center', justifyContent: 'center',
                        }}
                      >
                        <Typography variant="caption" color="text.disabled">—</Typography>
                      </Box>
                    )}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" fontWeight={500}>{event.name}</Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {formatDate(event.startDate)}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {event.venue || '—'}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <EventStatusChip status={event.status} />
                  </TableCell>
                  <TableCell align="right">
                    <Typography variant="body2" fontWeight={500}>
                      {event.totalRegistrations?.toLocaleString() ?? '—'}
                    </Typography>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* ── Feedback Snackbar ── */}
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
