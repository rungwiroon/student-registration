import { test, expect } from '@playwright/test';
import { randomBytes } from 'node:crypto';

/**
 * API contract tests for changing a staff member's LINE id (PUT /api/backoffice/staff/{id}).
 * Requires the backend in mock-auth mode with seed data: the default mock user is a Teacher
 * (and a registered guardian). "mock-token:<id>" acts as that LINE user.
 */

test.describe('Staff LINE id', () => {
  const teacher = { Authorization: 'Bearer mock-token' };
  const asUser = (lineUserId) => ({ Authorization: `Bearer mock-token:${lineUserId}` });
  const newLineId = () => `U${randomBytes(16).toString('hex')}`;

  const createStaff = async (request, overrides = {}) => {
    const lineUserId = overrides.lineUserId ?? newLineId();
    const response = await request.post('/api/backoffice/staff', {
      headers: teacher,
      data: { lineUserId, name: 'e2e staff', role: 'ParentNetworkStaff', position: 'ประธาน', ...overrides }
    });
    expect(response.status()).toBe(200);
    const body = await response.json();
    return { id: body.id, lineUserId };
  };

  const relink = (request, id, lineUserId, headers = teacher) =>
    request.put(`/api/backoffice/staff/${id}`, { headers, data: { lineUserId } });

  test('moving a staff row to a new LINE id gives access to the new id and removes it from the old', async ({ request }) => {
    const staff = await createStaff(request);
    const correctId = newLineId();

    expect((await request.get('/api/backoffice/me', { headers: asUser(staff.lineUserId) })).status()).toBe(200);

    const response = await relink(request, staff.id, correctId);
    expect(response.status()).toBe(200);
    expect((await response.json()).lineUserId).toBe(correctId);

    expect((await request.get('/api/backoffice/me', { headers: asUser(correctId) })).status()).toBe(200);
    expect((await request.get('/api/backoffice/me', { headers: asUser(staff.lineUserId) })).status()).toBe(403);
  });

  test('other fields saved in the same request are kept', async ({ request }) => {
    const staff = await createStaff(request);
    const correctId = newLineId();

    const response = await request.put(`/api/backoffice/staff/${staff.id}`, {
      headers: teacher,
      data: { lineUserId: correctId, name: 'ชื่อใหม่', position: 'รองประธาน' }
    });
    expect(response.status()).toBe(200);

    const list = await (await request.get('/api/backoffice/staff', { headers: teacher })).json();
    const row = list.find((s) => s.id === staff.id);
    expect(row.lineUserId).toBe(correctId);
    expect(row.name).toBe('ชื่อใหม่');
    expect(row.position).toBe('รองประธาน');
    // The row was moved, not duplicated.
    expect(list.filter((s) => s.lineUserId === staff.lineUserId)).toHaveLength(0);
  });

  test('keeps the row when the same id is sent again (and never validates an unchanged id)', async ({ request }) => {
    const staff = await createStaff(request);
    const response = await relink(request, staff.id, staff.lineUserId);
    expect(response.status()).toBe(200);

    // The seeded teacher uses a non-"U…" mock id; saving other fields with it must still work.
    const list = await (await request.get('/api/backoffice/staff', { headers: teacher })).json();
    const self = list.find((s) => s.isSelf);
    const keep = await request.put(`/api/backoffice/staff/${self.id}`, {
      headers: teacher,
      data: { lineUserId: self.lineUserId, position: 'ครูที่ปรึกษา' }
    });
    expect(keep.status()).toBe(200);
  });

  test('rejects an id held by another active staff row', async ({ request }) => {
    const a = await createStaff(request);
    const b = await createStaff(request);

    const response = await relink(request, a.id, b.lineUserId);
    expect(response.status()).toBe(409);
    expect(await response.text()).toContain('e2e staff');
    expect((await request.get('/api/backoffice/me', { headers: asUser(a.lineUserId) })).status()).toBe(200);
  });

  test('rejects an id held by a deactivated staff row (UNIQUE covers inactive rows)', async ({ request }) => {
    const a = await createStaff(request);
    const b = await createStaff(request);
    expect((await request.delete(`/api/backoffice/staff/${b.id}`, { headers: teacher })).status()).toBe(200);

    const response = await relink(request, a.id, b.lineUserId);
    expect(response.status()).toBe(409);
    expect(await response.text()).toContain('ปิดใช้งาน');
  });

  test('cannot change your own LINE id', async ({ request }) => {
    const list = await (await request.get('/api/backoffice/staff', { headers: teacher })).json();
    const self = list.find((s) => s.isSelf);
    expect(self).toBeTruthy();

    const response = await relink(request, self.id, newLineId());
    expect(response.status()).toBe(400);
    expect((await request.get('/api/backoffice/me', { headers: teacher })).status()).toBe(200);
  });

  for (const [label, value] of [
    ['too short', 'U123'],
    ['no U prefix', 'a'.repeat(33)],
    ['uppercase hex', `U${'A'.repeat(32)}`],
    ['with spaces inside', `U${'a'.repeat(16)} ${'a'.repeat(16)}`],
  ]) {
    test(`rejects a malformed LINE id: ${label}`, async ({ request }) => {
      const staff = await createStaff(request);
      const response = await relink(request, staff.id, value);
      expect(response.status()).toBe(400);
      expect(await response.text()).toContain('LINE ID');
    });
  }

  test('adding staff with a malformed LINE id is rejected', async ({ request }) => {
    const response = await request.post('/api/backoffice/staff', {
      headers: teacher,
      data: { lineUserId: 'not-a-line-id', name: 'x', role: 'Teacher' }
    });
    expect(response.status()).toBe(400);
  });

  test('only a Teacher can change a LINE id', async ({ request }) => {
    const network = await createStaff(request);
    const target = await createStaff(request);

    const response = await relink(request, target.id, newLineId(), asUser(network.lineUserId));
    expect(response.status()).toBe(403);
  });

  test('unknown staff id is 404', async ({ request }) => {
    const response = await relink(request, 'does-not-exist', newLineId());
    expect(response.status()).toBe(404);
  });

  test('staff list names the registered guardian behind a LINE id', async ({ request }) => {
    const list = await (await request.get('/api/backoffice/staff', { headers: teacher })).json();

    // The seeded teacher is also a registered guardian: name comes from the stored registration data.
    const self = list.find((s) => s.isSelf);
    expect(self.isRegisteredGuardian).toBe(true);
    expect(self.guardianName).toBeTruthy();

    const stranger = await createStaff(request);
    const row = (await (await request.get('/api/backoffice/staff', { headers: teacher })).json()).find((s) => s.id === stranger.id);
    expect(row.isRegisteredGuardian).toBe(false);
    expect(row.guardianName).toBeNull();
  });
});
