import apiClient from './apiClient';

/**
 * Returns all synced events ordered by start date descending.
 * @returns {Promise<Array>}
 */
export async function getEvents() {
  const { data } = await apiClient.get('/api/v1/events');
  return data;
}

/**
 * Returns full event detail including ticket types.
 * @param {number} id
 * @returns {Promise<object>}
 */
export async function getEventById(id) {
  const { data } = await apiClient.get(`/api/v1/events/${id}`);
  return data;
}

/**
 * Enqueues an Eventbrite event sync job on the backend.
 * @returns {Promise<void>}
 */
export async function syncEvents() {
  await apiClient.post('/api/v1/events/sync');
}
