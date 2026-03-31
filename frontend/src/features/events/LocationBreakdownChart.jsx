import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, Cell,
} from 'recharts';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

const BAR_COLOR = '#1976d2';

function formatTooltip(value, name, props) {
  return [`${value.toLocaleString()} (${props.payload.percentage}%)`, 'Registrations'];
}

export default function LocationBreakdownChart({ data = [] }) {
  if (data.length === 0) {
    return (
      <Box sx={{ py: 6, textAlign: 'center' }}>
        <Typography variant="body2" color="text.secondary">
          No location data available. Ensure ticket types follow the &quot;Location — Type&quot; naming convention.
        </Typography>
      </Box>
    );
  }

  // Sort descending by count (data should arrive sorted, but be defensive)
  const sorted = [...data].sort((a, b) => b.count - a.count);
  const chartHeight = Math.max(sorted.length * 44, 280);

  return (
    <ResponsiveContainer width="100%" height={chartHeight}>
      <BarChart
        data={sorted}
        layout="vertical"
        margin={{ top: 4, right: 40, left: 8, bottom: 4 }}
      >
        <CartesianGrid strokeDasharray="3 3" horizontal={false} />
        <XAxis
          type="number"
          allowDecimals={false}
          tick={{ fontSize: 12 }}
          tickLine={false}
          axisLine={false}
        />
        <YAxis
          type="category"
          dataKey="location"
          width={150}
          tick={{ fontSize: 12 }}
          tickLine={false}
        />
        <Tooltip formatter={formatTooltip} />
        <Bar dataKey="count" radius={[0, 4, 4, 0]}>
          {sorted.map((entry, i) => (
            <Cell
              key={entry.location}
              fill={entry.location === 'Other' ? '#9e9e9e' : BAR_COLOR}
              opacity={1 - i * 0.04}
            />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
