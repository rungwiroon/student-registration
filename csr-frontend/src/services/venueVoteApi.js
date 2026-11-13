import { apiJson } from './apiClient';

export async function fetchVenueVote(token) {
  return apiJson('/api/venue-vote', token);
}

export async function submitVenueVote(token, venueId, attendeeCount) {
  return apiJson('/api/venue-vote', token, {
    method: 'PUT',
    body: JSON.stringify({ venueId, attendeeCount }),
    headers: { 'Content-Type': 'application/json' }
  });
}
