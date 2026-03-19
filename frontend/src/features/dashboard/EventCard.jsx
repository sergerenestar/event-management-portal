import { useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import CardActionArea from '@mui/material/CardActionArea';
import CardContent from '@mui/material/CardContent';
import LinearProgress from '@mui/material/LinearProgress';
import Typography from '@mui/material/Typography';
import CalendarTodayIcon from '@mui/icons-material/CalendarToday';
import LocationOnIcon from '@mui/icons-material/LocationOn';
import EventStatusChip from '../events/EventStatusChip';

function formatDate(iso) {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
}

export default function EventCard({ event }) {
  const navigate = useNavigate();
  const fillRate = event.totalCapacity > 0
    ? Math.min((event.totalRegistrations / event.totalCapacity) * 100, 100)
    : 0;

  return (
    <Card
      variant="outlined"
      sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 2, transition: 'box-shadow 0.2s', '&:hover': { boxShadow: 3 } }}
    >
      <CardActionArea
        onClick={() => navigate(`/events/${event.id}`)}
        sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', alignItems: 'stretch' }}
      >
        {/* Thumbnail */}
        {event.thumbnailUrl ? (
          <Box
            component="img"
            src={event.thumbnailUrl}
            alt={event.name}
            sx={{ width: '100%', height: 140, objectFit: 'cover' }}
          />
        ) : (
          <Box
            sx={{
              width: '100%', height: 140,
              bgcolor: 'grey.100',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}
          >
            <Typography variant="caption" color="text.disabled">No image</Typography>
          </Box>
        )}

        <CardContent sx={{ flexGrow: 1, pb: '12px !important' }}>
          {/* Status chip + name */}
          <Box display="flex" alignItems="flex-start" justifyContent="space-between" gap={1} mb={0.75}>
            <Typography
              variant="subtitle2"
              fontWeight={600}
              sx={{ lineHeight: 1.3, flex: 1,
                overflow: 'hidden', display: '-webkit-box',
                WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}
            >
              {event.name}
            </Typography>
            <EventStatusChip status={event.status} />
          </Box>

          {/* Date */}
          <Box display="flex" alignItems="center" gap={0.5} mb={0.5}>
            <CalendarTodayIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
            <Typography variant="caption" color="text.secondary">{formatDate(event.startDate)}</Typography>
          </Box>

          {/* Venue */}
          {event.venue && (
            <Box display="flex" alignItems="center" gap={0.5} mb={1.25}>
              <LocationOnIcon sx={{ fontSize: 13, color: 'text.disabled' }} />
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ overflow: 'hidden', whiteSpace: 'nowrap', textOverflow: 'ellipsis' }}
              >
                {event.venue}
              </Typography>
            </Box>
          )}

          {/* Registration progress */}
          <Box mt="auto">
            <Box display="flex" justifyContent="space-between" mb={0.5}>
              <Typography variant="caption" color="text.secondary">Registrations</Typography>
              <Typography variant="caption" fontWeight={500}>
                {event.totalRegistrations.toLocaleString()}
                {event.totalCapacity > 0 && ` / ${event.totalCapacity.toLocaleString()}`}
              </Typography>
            </Box>
            <LinearProgress
              variant="determinate"
              value={fillRate}
              sx={{ height: 5, borderRadius: 3 }}
              color={fillRate >= 90 ? 'error' : fillRate >= 60 ? 'warning' : 'primary'}
            />
          </Box>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
