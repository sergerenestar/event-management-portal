import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import BottomNavigation from '@mui/material/BottomNavigation';
import BottomNavigationAction from '@mui/material/BottomNavigationAction';
import Drawer from '@mui/material/Drawer';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import MoreHorizIcon from '@mui/icons-material/MoreHoriz';
import NAV_ITEMS from './navItems';

// How many items appear directly in the bar; the rest go into "More"
const VISIBLE_COUNT = 4;

function getActiveIndex(pathname, items) {
  return items.findIndex(({ path }) =>
    path === '/dashboard' ? pathname === '/dashboard' : pathname.startsWith(path)
  );
}

export default function BottomNav() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const [moreOpen, setMoreOpen] = useState(false);

  const visibleItems  = NAV_ITEMS.slice(0, VISIBLE_COUNT);
  const overflowItems = NAV_ITEMS.slice(VISIBLE_COUNT);

  const activeIdx  = getActiveIndex(pathname, NAV_ITEMS);
  const inOverflow = activeIdx >= VISIBLE_COUNT;
  // navValue: index of visible slot, or VISIBLE_COUNT when an overflow route is active
  const navValue = inOverflow ? VISIBLE_COUNT : (activeIdx === -1 ? false : activeIdx);

  function handleOverflowNav(path) {
    navigate(path);
    setMoreOpen(false);
  }

  return (
    <>
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
        <BottomNavigation value={navValue} showLabels>
          {visibleItems.map(({ label, Icon }, idx) => (
            <BottomNavigationAction
              key={label}
              label={label}
              icon={<Icon />}
              onClick={() => navigate(NAV_ITEMS[idx].path)}
            />
          ))}

          {overflowItems.length > 0 && (
            <BottomNavigationAction
              value={VISIBLE_COUNT}
              label="More"
              icon={<MoreHorizIcon />}
              onClick={() => setMoreOpen(true)}
            />
          )}
        </BottomNavigation>
      </Paper>

      {/* Overflow sheet — slides up from bottom */}
      <Drawer
        anchor="bottom"
        open={moreOpen}
        onClose={() => setMoreOpen(false)}
        sx={{ display: { md: 'none' } }}
        PaperProps={{
          sx: { borderTopLeftRadius: 12, borderTopRightRadius: 12, pb: 2 },
        }}
      >
        <Typography
          variant="caption"
          color="text.secondary"
          sx={{ px: 2, pt: 2, pb: 1, display: 'block', fontWeight: 600, letterSpacing: 0.5 }}
        >
          MORE
        </Typography>

        <List disablePadding>
          {overflowItems.map(({ label, path, Icon }) => {
            const active = pathname === path || pathname.startsWith(path + '/');
            return (
              <ListItem key={path} disablePadding>
                <ListItemButton
                  onClick={() => handleOverflowNav(path)}
                  selected={active}
                  sx={{
                    px: 2,
                    '&.Mui-selected': {
                      bgcolor: 'primary.50',
                      color: 'primary.main',
                      '& .MuiListItemIcon-root': { color: 'primary.main' },
                    },
                  }}
                >
                  <ListItemIcon sx={{ minWidth: 40, color: active ? 'primary.main' : 'text.secondary' }}>
                    <Icon fontSize="small" />
                  </ListItemIcon>
                  <ListItemText
                    primary={label}
                    primaryTypographyProps={{ variant: 'body2', fontWeight: active ? 600 : 400 }}
                  />
                </ListItemButton>
              </ListItem>
            );
          })}
        </List>
      </Drawer>
    </>
  );
}
