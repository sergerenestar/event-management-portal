import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Breadcrumbs from '@mui/material/Breadcrumbs';
import Link from '@mui/material/Link';
import Typography from '@mui/material/Typography';
import Skeleton from '@mui/material/Skeleton';
import Paper from '@mui/material/Paper';
import Grid from '@mui/material/Grid';
import Alert from '@mui/material/Alert';
import Divider from '@mui/material/Divider';
import RegistrationTrendChart from './RegistrationTrendChart';
import LocationBreakdownChart from './LocationBreakdownChart';
import AttendeeTypeChart from './AttendeeTypeChart';
import { getDailyTrends, getLocationBreakdown, getAttendeeTypeBreakdown } from '../../services/registrationService';
import { getEventById } from '../../services/eventService';

function formatSyncTime(isoString) {
  if (!isoString) return null;
  return new Date(isoString).toLocaleString(undefined, {
    day: 'numeric', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function ChartSection({ title, loading, error, children }) {
  return (
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Typography variant="subtitle1" fontWeight={600} mb={2}>{title}</Typography>
      <Divider sx={{ mb: 2 }} />
      {loading ? (
        <Skeleton variant="rectangular" width="100%" height={300} sx={{ borderRadius: 1 }} />
      ) : error ? (
        <Alert severity="error" sx={{ mt: 1 }}>{error}</Alert>
      ) : (
        children
      )}
    </Paper>
  );
}

export default function EventAnalyticsPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const eventId = Number(id);

  const [loading, setLoading]       = useState(true);
  const [event, setEvent]           = useState(null);
  const [dailyTrends, setDailyTrends]           = useState([]);
  const [locationData, setLocationData]         = useState(null);
  const [attendeeTypeData, setAttendeeTypeData] = useState(null);
  const [errors, setErrors] = useState({ trends: null, location: null, attendeeType: null, event: null });

  useEffect(() => {
    async function loadAll() {
      setLoading(true);
      const [eventRes, trendsRes, locationRes, attendeeTypeRes] = await Promise.allSettled([
        getEventById(eventId),
        getDailyTrends(eventId),
        getLocationBreakdown(eventId),
        getAttendeeTypeBreakdown(eventId),
      ]);

      setErrors({
        event:       eventRes.status       === 'rejected' ? 'Failed to load event details.' : null,
        trends:      trendsRes.status      === 'rejected' ? 'Failed to load registration trend data.' : null,
        location:    locationRes.status    === 'rejected' ? 'Failed to load location breakdown.' : null,
        attendeeType: attendeeTypeRes.status === 'rejected' ? 'Failed to load attendee type breakdown.' : null,
      });

      if (eventRes.status === 'fulfilled')           setEvent(eventRes.value);
      if (trendsRes.status === 'fulfilled')          setDailyTrends(trendsRes.value);
      if (locationRes.status === 'fulfilled')        setLocationData(locationRes.value);
      if (attendeeTypeRes.status === 'fulfilled')    setAttendeeTypeData(attendeeTypeRes.value);

      setLoading(false);
    }
    loadAll();
  }, [eventId]);

  const lastSynced = locationData?.lastSyncedAt || attendeeTypeData?.lastSyncedAt;

  return (
    <Box sx={{ p: 3 }}>
      {/* Breadcrumb */}
      <Breadcrumbs sx={{ mb: 2 }}>
        <Link
          component="button"
          variant="body2"
          underline="hover"
          color="inherit"
          onClick={() => navigate('/events')}
          sx={{ cursor: 'pointer' }}
        >
          Events
        </Link>
        <Typography variant="body2" color="text.primary">
          {event?.name ?? 'Analytics'}
        </Typography>
      </Breadcrumbs>

      {/* Header */}
      <Box mb={3}>
        {loading ? (
          <Skeleton width={300} height={36} />
        ) : (
          <Typography variant="h5" fontWeight={600}>
            {event?.name ?? `Event ${id}`}
          </Typography>
        )}
        {lastSynced && (
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Data as of {formatSyncTime(lastSynced)}
          </Typography>
        )}
      </Box>

      {/* Charts grid */}
      <Grid container spacing={3}>
        {/* Registration Trend — full width */}
        <Grid item xs={12}>
          <ChartSection title="Registration Trend" loading={loading} error={errors.trends}>
            <RegistrationTrendChart data={dailyTrends} />
          </ChartSection>
        </Grid>

        {/* Location Breakdown — half width on md+ */}
        <Grid item xs={12} md={7}>
          <ChartSection title="Registrations by Location" loading={loading} error={errors.location}>
            <LocationBreakdownChart data={locationData?.locations ?? []} />
          </ChartSection>
        </Grid>

        {/* Attendee Type — half width on md+ */}
        <Grid item xs={12} md={5}>
          <ChartSection title="Adults vs Children" loading={loading} error={errors.attendeeType}>
            <AttendeeTypeChart data={attendeeTypeData?.breakdown ?? []} />
          </ChartSection>
        </Grid>
      </Grid>
    </Box>
  );
}
