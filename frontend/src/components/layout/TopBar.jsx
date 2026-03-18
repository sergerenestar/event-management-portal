import AppBar from '@mui/material/AppBar';
import Toolbar from '@mui/material/Toolbar';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import Box from '@mui/material/Box';
import { useAuth } from '../../features/auth/useAuth';

export default function TopBar() {
  const { admin, logout } = useAuth();

  return (
    <AppBar position="static">
      <Toolbar>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Event Portal
        </Typography>

        {admin && (
          <Box display="flex" alignItems="center" gap={2}>
            <Typography variant="body2">{admin.displayName}</Typography>
            <Button color="inherit" variant="outlined" size="small" onClick={logout}>
              Sign Out
            </Button>
          </Box>
        )}
      </Toolbar>
    </AppBar>
  );
}
