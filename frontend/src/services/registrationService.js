import apiClient from './apiClient';

const base = (eventId) => `/api/v1/events/${eventId}/registrations`;

/**
 * Returns aggregated registration totals and fill rate for an event.
 * @param {number} eventId
 * @returns {Promise<{totalRegistrations: number, totalCapacity: number, fillRate: number, lastSyncAt: string|null}>}
 */
export async function getSummary(eventId) {
  const { data } = await apiClient.get(`${base(eventId)}/summary`);
  return data;
}

/**
 * Returns registration count and fill percentage broken down by ticket type.
 * @param {number} eventId
 * @returns {Promise<Array<{ticketTypeId: number, name: string, quantitySold: number, capacity: number, fillPercentage: number, price: number, currency: string}>>}
 */
export async function getByTicketType(eventId) {
  const { data } = await apiClient.get(`${base(eventId)}/by-ticket-type`);
  return data;
}

/**
 * Returns daily registration snapshot data for trend charts.
 * @param {number} eventId
 * @returns {Promise<Array<{date: string, count: number, ticketTypeName: string, ticketTypeId: number}>>}
 */
export async function getDailyTrends(eventId) {
  const { data } = await apiClient.get(`${base(eventId)}/daily-trends`);
  return data;
}

/**
 * Enqueues a registration sync job for the specified event.
 * @param {number} eventId
 * @returns {Promise<void>}
 */
export async function syncRegistrations(eventId) {
  await apiClient.post(`${base(eventId)}/sync`);
}

/**
 * Returns registration counts grouped by location (parsed from ticket type names).
 * @param {number} eventId
 * @returns {Promise<{eventId: number, totalRegistrations: number, lastSyncedAt: string|null, locations: Array<{location: string, count: number, percentage: number}>}>}
 */
export async function getLocationBreakdown(eventId) {
  const { data } = await apiClient.get(`${base(eventId)}/location-breakdown`);
  return data;
}

/**
 * Returns registration counts split by attendee type (Adult/Children/Other).
 * @param {number} eventId
 * @returns {Promise<{eventId: number, totalRegistrations: number, lastSyncedAt: string|null, breakdown: Array<{attendeeType: string, count: number, percentage: number}>}>}
 */
export async function getAttendeeTypeBreakdown(eventId) {
  const { data } = await apiClient.get(`${base(eventId)}/attendee-type-breakdown`);
  return data;
}
