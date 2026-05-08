import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer,
} from 'recharts';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

const LINE_COLORS = ['#1976d2', '#388e3c', '#f57c00', '#7b1fa2', '#c62828', '#00838f'];

function pivotData(snapshots) {
  const dateMap = new Map();
  const seriesNames = new Set();
  for (const s of snapshots) {
    const label = s.ticketTypeName;
    seriesNames.add(label);
    if (!dateMap.has(s.date)) dateMap.set(s.date, { date: s.date });
    dateMap.get(s.date)[label] = s.count;
  }
  const rows = Array.from(dateMap.values()).sort((a, b) => a.date.localeCompare(b.date));
  return { rows, seriesNames: Array.from(seriesNames) };
}

function formatDate(isoDate) {
  const [year, month, day] = isoDate.split('-');
  return `${month}/${day}/${year.slice(2)}`;
}

export default function RegistrationTrendChart({ data = [] }) {
  if (data.length === 0) {
    return (
      <Box sx={{ py: 6, textAlign: 'center' }}>
        <Typography variant="body2" color="text.secondary">
          No registration trend data yet. Sync registrations to populate this chart.
        </Typography>
      </Box>
    );
  }

  const { rows, seriesNames } = pivotData(data);

  return (
    <ResponsiveContainer width="100%" height={320}>
      <LineChart data={rows} margin={{ top: 8, right: 24, left: 0, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
        <XAxis
          dataKey="date"
          tickFormatter={formatDate}
          tick={{ fontSize: 12 }}
          tickLine={false}
        />
        <YAxis
          allowDecimals={false}
          tick={{ fontSize: 12 }}
          tickLine={false}
          axisLine={false}
          width={40}
        />
        <Tooltip
          formatter={(value, name) => [value.toLocaleString(), name]}
          labelFormatter={(label) => `Date: ${label}`}
          contentStyle={{ borderRadius: 8, border: '1px solid #e0e0e0' }}
        />
        <Legend wrapperStyle={{ fontSize: 13 }} />
        {seriesNames.map((name, i) => (
          <Line
            key={name}
            type="monotone"
            dataKey={name}
            stroke={LINE_COLORS[i % LINE_COLORS.length]}
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 5 }}
          />
        ))}
      </LineChart>
    </ResponsiveContainer>
  );
}
