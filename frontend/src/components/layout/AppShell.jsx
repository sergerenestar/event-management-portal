import Box from '@mui/material/Box';
import BottomNav from './BottomNav';
import Sidebar from './Sidebar';
import TopBar from './TopBar';

export default function AppShell({ children }) {
  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      {/* Sidebar: desktop only */}
      <Sidebar />

      {/* Right column: TopBar + scrollable content */}
      <Box sx={{ display: 'flex', flexDirection: 'column', flexGrow: 1, minWidth: 0 }}>
        <TopBar />
        <Box
          component="main"
          sx={{
            flexGrow: 1,
            overflow: 'auto',
            bgcolor: 'grey.50',
            minHeight: 0,
            // clearance for fixed bottom nav on mobile
            pb: { xs: 7, md: 0 },
          }}
        >
          {children}
        </Box>
      </Box>

      {/* Bottom nav: mobile only */}
      <BottomNav />
    </Box>
  );
}
