import Chip from '@mui/material/Chip';

const STATUS_CONFIG = {
  live:      { label: 'Live',      color: 'success' },
  ended:     { label: 'Ended',     color: 'default' },
  draft:     { label: 'Draft',     color: 'warning' },
  cancelled: { label: 'Cancelled', color: 'error'   },
};

export default function EventStatusChip({ status }) {
  const cfg = STATUS_CONFIG[status?.toLowerCase()] ?? { label: status ?? 'Unknown', color: 'default' };
  return <Chip label={cfg.label} color={cfg.color} size="small" />;
}
