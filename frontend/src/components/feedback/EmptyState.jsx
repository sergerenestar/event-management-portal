import Typography from '@mui/material/Typography';

export default function EmptyState({ message }) {
  return <Typography color="text.secondary">{message || 'No data available.'}</Typography>;
}
