import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';

const TYPE_COLORS = {
  Adult:    '#1976d2',
  Children: '#388e3c',
  Other:    '#9e9e9e',
};

function customLabel({ name: _name, value, percent }) {
  return `${value.toLocaleString()} (${(percent * 100).toFixed(1)}%)`;
}

export default function AttendeeTypeChart({ data = [] }) {
  if (data.length === 0) {
    return (
      <Box sx={{ py: 6, textAlign: 'center' }}>
        <Typography variant="body2" color="text.secondary">
          No attendee type data available.
        </Typography>
      </Box>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={300}>
      <PieChart>
        <Pie
          data={data}
          dataKey="count"
          nameKey="attendeeType"
          cx="50%"
          cy="45%"
          innerRadius="45%"
          outerRadius="70%"
          label={customLabel}
          labelLine={true}
        >
          {data.map((entry) => (
            <Cell
              key={entry.attendeeType}
              fill={TYPE_COLORS[entry.attendeeType] ?? '#bdbdbd'}
            />
          ))}
        </Pie>
        <Tooltip
          formatter={(value, name) => [value.toLocaleString(), name]}
        />
        <Legend
          formatter={(value) => `${value}`}
          wrapperStyle={{ fontSize: 13, paddingTop: 8 }}
        />
      </PieChart>
    </ResponsiveContainer>
  );
}
