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
