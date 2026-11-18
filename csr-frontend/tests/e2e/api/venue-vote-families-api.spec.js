import { test, expect } from '@playwright/test';
import { randomBytes } from 'node:crypto';

/**
 * GET /api/backoffice/venue-vote/families: who goes where, with how many people.
 * Requires the backend in mock-auth mode with seed data (default mock user is a Teacher).
 */

const run = Date.now().toString(36);
const teacher = { Authorization: 'Bearer mock-token' };
const as = (lineId) => ({ Authorization: `Bearer mock-token:${lineId}` });
const familiesUrl = '/api/backoffice/venue-vote/families';

let seq = 0;
async function registerFamily(request, { studentId, lastName = 'ใจดีมาก' } = {}) {
  seq += 1;
  const tag = `${run}-${seq}`;
  const lineId = `e2e-fam-${tag}`;
  const code = studentId ?? `F${tag}`;
  const payload = {
    student: { studentId: code, firstName: 'สมศักดิ์', lastName, phone: `081${String(3000000 + seq).slice(-7)}`, newRoom: '9/9', newNo: seq },
    guardians: [{ order: 1, relationType: 'Father', firstName: 'พ่อ', lastName: tag, phone: '0830000001' }],
  };
  const response = await request.post('/api/register', { headers: as(lineId), multipart: { payload: JSON.stringify(payload) } });
  expect(response.status(), await response.text()).toBe(200);
  return { code, lineId, headers: as(lineId) };
}

const vote = (request, family, venueId, attendeeCount) =>
  request.put('/api/venue-vote', { headers: family.headers, data: { venueId, attendeeCount } });

const rowFor = async (request, code, headers = teacher) => {
  const response = await request.get(familiesUrl, { headers });
  expect(response.status()).toBe(200);
  return (await response.json()).find((r) => r.studentId === code);
};

test.describe('Venue vote: per-family list', () => {
  test('requires auth, and a non-staff LINE user gets 403', async ({ request }) => {
    expect((await request.get(familiesUrl)).status()).toBe(401);
    expect((await request.get(familiesUrl, { headers: as('e2e-not-staff') })).status()).toBe(403);
  });

  test('shows venue and head-count for a family that voted, and "not voted" for one that did not', async ({ request }) => {
    const voter = await registerFamily(request);
    const silent = await registerFamily(request);
    expect((await vote(request, voter, 'blu-o', 6)).status()).toBe(200);

    const voted = await rowFor(request, voter.code);
    expect(voted).toMatchObject({ hasVoted: true, venueId: 'blu-o', attendeeCount: 6, newRoom: '9/9' });
    expect(voted.votedAtUtc).toBeTruthy();

    const notVoted = await rowFor(request, silent.code);
    expect(notVoted).toMatchObject({ hasVoted: false, venueId: null, attendeeCount: null });
  });

  test('changing the vote updates the same row instead of adding one', async ({ request }) => {
    const family = await registerFamily(request);
    await vote(request, family, 'big-tree', 3);
    await vote(request, family, 'suan-lung-mong', 8);

    const rows = (await (await request.get(familiesUrl, { headers: teacher })).json()).filter((r) => r.studentId === family.code);
    expect(rows).toHaveLength(1);
    expect(rows[0]).toMatchObject({ venueId: 'suan-lung-mong', attendeeCount: 8 });
  });

  test('the test student 12345 is not listed even after voting', async ({ request }) => {
    const tester = await registerFamily(request, { studentId: '12345' });
    expect((await vote(request, tester, 'big-tree', 4)).status()).toBe(200);

    const response = await request.get(familiesUrl, { headers: teacher });
    expect((await response.json()).some((r) => r.studentId === '12345')).toBe(false);
  });

  test('people in the list add up to the summary total', async ({ request }) => {
    const family = await registerFamily(request);
    await vote(request, family, 'lets-chill', 5);

    const rows = await (await request.get(familiesUrl, { headers: teacher })).json();
    const summary = await (await request.get('/api/backoffice/venue-vote/summary', { headers: teacher })).json();

    expect(rows.filter((r) => r.hasVoted).length).toBe(summary.totalVotes);
    expect(rows.reduce((sum, r) => sum + (r.attendeeCount ?? 0), 0)).toBe(summary.totalPeople);
  });

  test('Teacher sees the full name, ParentNetworkStaff sees the last name masked', async ({ request }) => {
    const family = await registerFamily(request, { lastName: 'ใจดีมาก' });

    const networkLineId = `U${randomBytes(16).toString('hex')}`;
    const created = await request.post('/api/backoffice/staff', {
      headers: teacher,
      data: { lineUserId: networkLineId, name: 'e2e network', role: 'ParentNetworkStaff' },
    });
    expect(created.status()).toBe(200);

    const full = await rowFor(request, family.code);
    expect(full.name).toContain('ใจดีมาก');

    const masked = await rowFor(request, family.code, as(networkLineId));
    expect(masked.name).toContain('สมศักดิ์');
    expect(masked.name).not.toContain('ใจดีมาก');
  });
});
