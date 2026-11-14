import { test, expect } from '@playwright/test';

/**
 * The test student (รหัสประจำตัว 12345, VenueVote:ExcludedStudentCodes) may vote like anyone,
 * but the vote must not show up in the tally, the totals or the backoffice summary.
 * Requires the backend in mock-auth mode with the default appsettings.
 */

const tester = { Authorization: 'Bearer mock-token:e2e-test-student-parent' };
const teacher = { Authorization: 'Bearer mock-token' };
const apiUrl = '/api/venue-vote';

async function ensureTestStudent(request) {
  const payload = {
    student: { studentId: '12345', firstName: 'ทดสอบ', lastName: 'ระบบ', phone: '0899990000', newRoom: '1/2', newNo: 40 },
    guardians: [{ order: 1, relationType: 'Father', firstName: 'พ่อ', lastName: 'ทดสอบ', phone: '0830000001' }],
  };
  const response = await request.post('/api/register', { headers: tester, multipart: { payload: JSON.stringify(payload) } });
  expect(response.status(), await response.text()).toBe(200);
}

test.describe('Venue vote: test student is excluded', () => {
  test('its vote is stored for the family but not counted anywhere', async ({ request }) => {
    await ensureTestStudent(request);

    const beforeApp = await (await request.get(apiUrl, { headers: teacher })).json();
    const beforeBackoffice = await (await request.get('/api/backoffice/venue-vote/summary', { headers: teacher })).json();

    const vote = await request.put(apiUrl, { headers: tester, data: { venueId: 'suan-lung-mong', attendeeCount: 7 } });
    expect(vote.status()).toBe(200);
    const mine = await vote.json();
    // The tester still sees their own vote…
    expect(mine.myVenueId).toBe('suan-lung-mong');
    expect(mine.myAttendeeCount).toBe(7);

    // …but nothing counts it.
    const afterApp = await (await request.get(apiUrl, { headers: teacher })).json();
    expect(afterApp.totalVotes).toBe(beforeApp.totalVotes);
    expect(afterApp.totalPeople).toBe(beforeApp.totalPeople);
    expect(afterApp.counts).toEqual(beforeApp.counts);

    const afterBackoffice = await (await request.get('/api/backoffice/venue-vote/summary', { headers: teacher })).json();
    expect(afterBackoffice.totalVotes).toBe(beforeBackoffice.totalVotes);
    expect(afterBackoffice.totalPeople).toBe(beforeBackoffice.totalPeople);
    expect(afterBackoffice.counts).toEqual(beforeBackoffice.counts);
  });

  test('the test student is not part of the families denominator', async ({ request }) => {
    await ensureTestStudent(request);

    const students = await (await request.get('/api/backoffice/students', { headers: teacher })).json();
    expect(students.some((s) => s.studentId === '12345')).toBe(true);

    const summary = await (await request.get('/api/backoffice/venue-vote/summary', { headers: teacher })).json();
    expect(summary.totalFamilies).toBe(students.filter((s) => s.studentId !== '12345').length);
  });

  test('a normal student still counts', async ({ request }) => {
    const before = await (await request.get(apiUrl, { headers: teacher })).json();
    const vote = await request.put(apiUrl, { headers: teacher, data: { venueId: 'big-tree', attendeeCount: 2 } });
    expect(vote.status()).toBe(200);
    const after = await vote.json();
    // The seeded teacher may have voted already in another spec: either way the tally includes them.
    expect(after.totalVotes).toBeGreaterThanOrEqual(before.totalVotes ?? 0);
    expect(after.totalVotes).toBeGreaterThan(0);
  });
});
