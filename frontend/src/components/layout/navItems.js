import AssessmentIcon from '@mui/icons-material/Assessment';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import CampaignIcon from '@mui/icons-material/Campaign';
import DashboardIcon from '@mui/icons-material/Dashboard';
import ShareIcon from '@mui/icons-material/Share';
import VideoLibraryIcon from '@mui/icons-material/VideoLibrary';

// Single source of truth for navigation items.
// Add new entries here — Sidebar and BottomNav pick them up automatically.
// Bottom nav shows the first 4; extras go into the "More" overflow drawer.
const NAV_ITEMS = [
  { label: 'Dashboard',  path: '/dashboard',      Icon: DashboardIcon },
  { label: 'Events',     path: '/events',          Icon: CalendarMonthIcon },
  { label: 'Campaigns',  path: '/communications',  Icon: CampaignIcon },
  { label: 'Social',     path: '/social',          Icon: ShareIcon },
  { label: 'Sessions',   path: '/content',         Icon: VideoLibraryIcon },
  { label: 'Reports',    path: '/reports',         Icon: AssessmentIcon },
];

export default NAV_ITEMS;
