import Alert from '@mui/material/Alert';

export default function SuccessBanner({ message }) {
  return <Alert severity="success">{message || 'Operation successful.'}</Alert>;
}
