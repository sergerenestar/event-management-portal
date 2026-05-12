import { useLocation, useNavigate } from 'react-router-dom';
import BottomNavigation from '@mui/material/BottomNavigation';
import BottomNavigationAction from '@mui/material/BottomNavigationAction';
import Paper from '@mui/material/Paper';
import AssessmentIcon from '@mui/icons-material/Assessment';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import CampaignIcon from '@mui/icons-material/Campaign';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ShareIcon from '@mui/icons-material/Share';
import VideoLibraryIcon from '@mui/icons-material/VideoLibrary';

const NAV_ITEMS = [
  { label: 'Dashboard',  path: '/dashboard',      icon: <DashboardIcon /> },
  { label: 'Events',     path: '/events',          icon: <CalendarMonthIcon /> },
  { label: 'Campaigns',  path: '/communications',  icon: <CampaignIcon /> },
  { label: 'Social',     path: '/social',          icon: <ShareIcon /> },
  { label: 'Sessions',   path: '/content',         icon: <VideoLibraryIcon /> },
  { label: 'Reports',    path: '/reports',         icon: <AssessmentIcon /> },
];

function activeIndex(pathname) {
  const idx = NAV_ITEMS.findIndex(({ path }) =>
    path === '/dashboard' ? pathname === '/dashboard' : pathname.startsWith(path)
  );
  return idx === -1 ? 0 : idx;
}

export default function BottomNav() {
  const { pathname } = useLocation();
  const navigate = useNavigate();

  return (
    <Paper
      elevation={3}
      sx={{
        display: { xs: 'block', md: 'none' },
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        zIndex: (theme) => theme.zIndex.appBar,
      }}
    >
      <BottomNavigation
        value={activeIndex(pathname)}
        onChange={(_, idx) => navigate(NAV_ITEMS[idx].path)}
        showLabels
      >
        {NAV_ITEMS.map(({ label, icon }) => (
          <BottomNavigationAction key={label} label={label} icon={icon} />
        ))}
      </BottomNavigation>
    </Paper>
  );
}
