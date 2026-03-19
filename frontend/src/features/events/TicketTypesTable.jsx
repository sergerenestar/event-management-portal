import Box from '@mui/material/Box';
import LinearProgress from '@mui/material/LinearProgress';
import Paper from '@mui/material/Paper';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';

function formatPrice(price, currency) {
  if (!price) return 'Free';
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: currency || 'USD' }).format(price);
}

export default function TicketTypesTable({ ticketTypes = [] }) {
  return (
    <TableContainer component={Paper} variant="outlined">
      <Table size="small">
        <TableHead>
          <TableRow sx={{ bgcolor: 'grey.50' }}>
            <TableCell><Typography variant="subtitle2">Ticket Type</Typography></TableCell>
            <TableCell><Typography variant="subtitle2">Price</Typography></TableCell>
            <TableCell align="right"><Typography variant="subtitle2">Sold</Typography></TableCell>
            <TableCell align="right"><Typography variant="subtitle2">Capacity</Typography></TableCell>
            <TableCell sx={{ minWidth: 160 }}><Typography variant="subtitle2">Fill Rate</Typography></TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {ticketTypes.length === 0 ? (
            <TableRow>
              <TableCell colSpan={5}>
                <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
                  No ticket types found.
                </Typography>
              </TableCell>
            </TableRow>
          ) : (
            ticketTypes.map((tt) => (
              <TableRow key={tt.ticketTypeId} hover>
                <TableCell>
                  <Typography variant="body2" fontWeight={500}>{tt.name}</Typography>
                </TableCell>
                <TableCell>
                  <Typography variant="body2" color="text.secondary">
                    {formatPrice(tt.price, tt.currency)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2">{tt.quantitySold.toLocaleString()}</Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2" color="text.secondary">{tt.capacity.toLocaleString()}</Typography>
                </TableCell>
                <TableCell>
                  <Box display="flex" alignItems="center" gap={1}>
                    <LinearProgress
                      variant="determinate"
                      value={Math.min(tt.fillPercentage, 100)}
                      sx={{ flexGrow: 1, height: 6, borderRadius: 3 }}
                      color={tt.fillPercentage >= 90 ? 'error' : tt.fillPercentage >= 60 ? 'warning' : 'primary'}
                    />
                    <Typography variant="caption" color="text.secondary" sx={{ minWidth: 36, textAlign: 'right' }}>
                      {tt.fillPercentage.toFixed(1)}%
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
