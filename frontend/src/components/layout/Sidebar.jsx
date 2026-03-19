import { useLocation, useNavigate } from 'react-router-dom';
import Box from '@mui/material/Box';
import Divider from '@mui/material/Divider';
import Drawer from '@mui/material/Drawer';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Typography from '@mui/material/Typography';
import AssessmentIcon from '@mui/icons-material/Assessment';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import CampaignIcon from '@mui/icons-material/Campaign';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ShareIcon from '@mui/icons-material/Share';
import VideoLibraryIcon from '@mui/icons-material/VideoLibrary';

const DRAWER_WIDTH = 240;

const NAV_ITEMS = [
  { label: 'Dashboard',    path: '/dashboard',       icon: <DashboardIcon fontSize="small" /> },
  { label: 'Events',       path: '/events',          icon: <CalendarMonthIcon fontSize="small" /> },
  { label: 'Campaigns',    path: '/communications',  icon: <CampaignIcon fontSize="small" /> },
  { label: 'Social Posts', path: '/social',          icon: <ShareIcon fontSize="small" /> },
  { label: 'Sessions',     path: '/content',         icon: <VideoLibraryIcon fontSize="small" /> },
  { label: 'Reports',      path: '/reports',         icon: <AssessmentIcon fontSize="small" /> },
];

function isActive(pathname, itemPath) {
  if (itemPath === '/dashboard') return pathname === '/dashboard';
  return pathname === itemPath || pathname.startsWith(itemPath + '/');
}

export default function Sidebar() {
  const { pathname } = useLocation();
  const navigate = useNavigate();

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: DRAWER_WIDTH,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: DRAWER_WIDTH,
          boxSizing: 'border-box',
          borderRight: '1px solid',
          borderColor: 'divider',
          bgcolor: 'background.paper',
        },
      }}
    >
      {/* Logo / Brand */}
      <Box sx={{ px: 2.5, py: 2.25, display: 'flex', alignItems: 'center', gap: 1 }}>
        <CalendarMonthIcon color="primary" />
        <Typography variant="subtitle1" fontWeight={700} color="primary.main" noWrap>
          Event Portal
        </Typography>
      </Box>

      <Divider />

      {/* Nav items */}
      <List sx={{ px: 1, pt: 1 }}>
        {NAV_ITEMS.map(({ label, path, icon }) => {
          const active = isActive(pathname, path);
          return (
            <ListItem key={path} disablePadding sx={{ mb: 0.25 }}>
              <ListItemButton
                onClick={() => navigate(path)}
                selected={active}
                sx={{
                  borderRadius: 1.5,
                  '&.Mui-selected': {
                    bgcolor: 'primary.50',
                    color: 'primary.main',
                    '& .MuiListItemIcon-root': { color: 'primary.main' },
                    '&:hover': { bgcolor: 'primary.100' },
                  },
                }}
              >
                <ListItemIcon sx={{ minWidth: 36, color: active ? 'primary.main' : 'text.secondary' }}>
                  {icon}
                </ListItemIcon>
                <ListItemText
                  primary={label}
                  primaryTypographyProps={{
                    variant: 'body2',
                    fontWeight: active ? 600 : 400,
                  }}
                />
              </ListItemButton>
            </ListItem>
          );
        })}
      </List>
    </Drawer>
  );
}
