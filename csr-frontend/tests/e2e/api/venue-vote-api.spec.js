import { test, expect } from '@playwright/test';

/**
 * API contract tests for /api/venue-vote.
 * Requires the backend in mock-auth mode with seed data (the mock LINE user
 * is a registered guardian) and no VenueVote:ClosesAtUtc in the past.
 */

test.describe('Venue Vote API', () => {
  const apiUrl = '/api/venue-vote';
  const auth = { Authorization: 'Bearer mock-token' };
  const countOf = (body, id) => body.counts.find((c) => c.venueId === id);

  test('returns 401 without auth token', async ({ request }) => {
    const response = await request.get(apiUrl);
    expect(response.status()).toBe(401);
  });

  test('lists the four venues and the attendee limit', async ({ request }) => {
    const response = await request.get(apiUrl, { headers: auth });
    expect(response.status()).toBe(200);

    const body = await response.json();
    expect(body.venues.map((v) => v.id)).toEqual(['big-tree', 'lets-chill', 'blu-o', 'suan-lung-mong']);
    expect(body.isRegistered).toBe(true);
    expect(body.maxAttendees).toBe(10);
  });

  test('rejects an unknown venue', async ({ request }) => {
    const response = await request.put(apiUrl, { headers: auth, data: { venueId: 'nowhere', attendeeCount: 2 } });
    expect(response.status()).toBe(400);
  });

  for (const [label, attendeeCount] of [
    ['missing', undefined],
    ['null', null],
    ['zero', 0],
    ['negative', -1],
    ['above the limit', 11],
  ]) {
    test(`rejects ${label} attendee count`, async ({ request }) => {
      const response = await request.put(apiUrl, { headers: auth, data: { venueId: 'big-tree', attendeeCount } });
      expect(response.status()).toBe(400);
      expect(await response.text()).toContain('จำนวนคนที่จะไป');
    });
  }

  test('rejects a non-integer attendee count', async ({ request }) => {
    const response = await request.put(apiUrl, { headers: auth, data: { venueId: 'big-tree', attendeeCount: 2.5 } });
    expect(response.status()).toBe(400);
  });

  test('changing the vote moves family and people instead of adding', async ({ request }) => {
    const first = await request.put(apiUrl, { headers: auth, data: { venueId: 'big-tree', attendeeCount: 3 } });
    expect(first.status()).toBe(200);
    const afterFirst = await first.json();
    expect(afterFirst.myVenueId).toBe('big-tree');
    expect(afterFirst.myAttendeeCount).toBe(3);
    expect(afterFirst.counts).toHaveLength(4);

    const second = await request.put(apiUrl, { headers: auth, data: { venueId: 'blu-o', attendeeCount: 5 } });
    expect(second.status()).toBe(200);
    const afterSecond = await second.json();
    expect(afterSecond.myVenueId).toBe('blu-o');
    expect(afterSecond.myAttendeeCount).toBe(5);
    expect(afterSecond.totalVotes).toBe(afterFirst.totalVotes);

    expect(countOf(afterSecond, 'big-tree').count).toBe(countOf(afterFirst, 'big-tree').count - 1);
    expect(countOf(afterSecond, 'big-tree').people).toBe(countOf(afterFirst, 'big-tree').people - 3);
    expect(countOf(afterSecond, 'blu-o').count).toBe(countOf(afterFirst, 'blu-o').count + 1);
    expect(countOf(afterSecond, 'blu-o').people).toBe(countOf(afterFirst, 'blu-o').people + 5);
    expect(afterSecond.totalPeople).toBe(afterFirst.totalPeople + 2);

    const read = await request.get(apiUrl, { headers: auth });
    const readBody = await read.json();
    expect(readBody.myVenueId).toBe('blu-o');
    expect(readBody.myAttendeeCount).toBe(5);
  });

  // "mock-token:<id>" makes the mock-auth backend act as a LINE user the seed data does not know,
  // i.e. a parent who has not registered a student yet.
  test.describe('parent without a registered student', () => {
    const unregistered = { Authorization: 'Bearer mock-token:e2e-unregistered-parent' };

    test('can open the page data: venues are listed, isRegistered is false', async ({ request }) => {
      const response = await request.get(apiUrl, { headers: unregistered });
      expect(response.status()).toBe(200);

      const body = await response.json();
      expect(body.isRegistered).toBe(false);
      expect(body.myVenueId).toBeNull();
      expect(body.myAttendeeCount).toBeNull();
      expect(body.venues).toHaveLength(4);
    });

    test('cannot vote: 403 asking them to register first', async ({ request }) => {
      const before = await (await request.get(apiUrl, { headers: auth })).json();

      const response = await request.put(apiUrl, { headers: unregistered, data: { venueId: 'big-tree', attendeeCount: 2 } });
      expect(response.status()).toBe(403);
      expect(await response.text()).toContain('ลงทะเบียน');

      // The rejected vote must not have been stored.
      const after = await (await request.get(apiUrl, { headers: auth })).json();
      expect(after.totalVotes ?? 0).toBe(before.totalVotes ?? 0);
    });
  });

  test('changing only the attendee count keeps the vote count', async ({ request }) => {
    const before = await (await request.put(apiUrl, { headers: auth, data: { venueId: 'blu-o', attendeeCount: 2 } })).json();
    const after = await (await request.put(apiUrl, { headers: auth, data: { venueId: 'blu-o', attendeeCount: 4 } })).json();

    expect(after.totalVotes).toBe(before.totalVotes);
    expect(countOf(after, 'blu-o').count).toBe(countOf(before, 'blu-o').count);
    expect(countOf(after, 'blu-o').people).toBe(countOf(before, 'blu-o').people + 2);
  });
});
