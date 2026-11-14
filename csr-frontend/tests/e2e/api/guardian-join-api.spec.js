import { test, expect } from '@playwright/test';

/**
 * A second parent (own LINE account) joins a student the other parent registered.
 * Requires the backend in mock-auth mode; "mock-token:<id>" acts as that LINE user.
 * Each test builds its own family so the in-memory attempt limiter and stored rows never collide.
 */

const run = Date.now().toString(36);
const as = (lineId) => ({ Authorization: `Bearer mock-token:${lineId}` });

let seq = 0;
function newFamily({ withSecondGuardian = true } = {}) {
  seq += 1;
  const tag = `${run}-${seq}`;
  const studentPhone = `081${String(1000000 + seq).slice(-7)}`;
  const motherPhone = `082${String(2000000 + seq).slice(-7)}`;
  return {
    tag,
    studentCode: `J${tag}`,
    studentPhone,
    motherPhone,
    father: as(`e2e-father-${tag}`),
    mother: as(`e2e-mother-${tag}`),
    payload(extra = {}) {
      const guardians = [
        { order: 1, relationType: 'Father', firstName: 'พ่อ', lastName: tag, phone: '0830000001' },
      ];
      if (withSecondGuardian) {
        guardians.push({ order: 2, relationType: 'Mother', firstName: 'แม่', lastName: tag, phone: motherPhone });
      }
      return {
        student: { studentId: `J${tag}`, firstName: 'เด็ก', lastName: tag, phone: studentPhone, newRoom: '1/2', newNo: 5 },
        guardians,
        ...extra,
      };
    },
  };
}

async function register(request, family, headers = family.father, payload = family.payload()) {
  const response = await request.post('/api/register', { headers, multipart: { payload: JSON.stringify(payload) } });
  expect(response.status(), await response.text()).toBe(200);
}

const join = (request, headers, studentId, phone) =>
  request.post('/api/register/join', { headers, data: { studentId, phone } });

test.describe('Guardian join', () => {
  test('requires auth', async ({ request }) => {
    const response = await request.post('/api/register/join', { data: { studentId: 'x', phone: '0811111111' } });
    expect(response.status()).toBe(401);
  });

  test('mother joins with the student phone and shares the same student and vote', async ({ request }) => {
    const family = newFamily();
    await register(request, family);

    const before = await request.get('/api/venue-vote', { headers: family.mother });
    expect((await before.json()).isRegistered).toBe(false);

    const response = await join(request, family.mother, family.studentCode, family.studentPhone);
    expect(response.status(), await response.text()).toBe(200);

    const motherProfile = await (await request.get('/api/me', { headers: family.mother })).json();
    const fatherProfile = await (await request.get('/api/me', { headers: family.father })).json();
    expect(motherProfile.student.id).toBe(fatherProfile.student.id);
    expect(motherProfile.student.studentId).toBe(family.studentCode);

    const vote = await request.put('/api/venue-vote', {
      headers: family.mother,
      data: { venueId: 'blu-o', attendeeCount: 3 },
    });
    expect(vote.status()).toBe(200);
    const fatherView = await (await request.get('/api/venue-vote', { headers: family.father })).json();
    expect(fatherView.isRegistered).toBe(true);
    expect(fatherView.myVenueId).toBe('blu-o');
    expect(fatherView.myAttendeeCount).toBe(3);
  });

  test('accepts the phone typed with spaces and dashes', async ({ request }) => {
    const family = newFamily();
    await register(request, family);
    const spaced = `${family.studentPhone.slice(0, 3)}-${family.studentPhone.slice(3, 6)} ${family.studentPhone.slice(6)}`;
    const response = await join(request, family.mother, family.studentCode, spaced);
    expect(response.status(), await response.text()).toBe(200);
  });

  test("mother can also verify with the phone the father entered for her", async ({ request }) => {
    const family = newFamily();
    await register(request, family);
    const response = await join(request, family.mother, family.studentCode, family.motherPhone);
    expect(response.status(), await response.text()).toBe(200);
  });

  test('father registered only himself: joining creates the second guardian row', async ({ request }) => {
    const family = newFamily({ withSecondGuardian: false });
    await register(request, family);

    const response = await join(request, family.mother, family.studentCode, family.studentPhone);
    expect(response.status(), await response.text()).toBe(200);

    const profile = await (await request.get('/api/me', { headers: family.father })).json();
    expect(profile.guardians.map((g) => g.order).sort()).toEqual([1, 2]);
  });

  test('wrong phone and unknown student code get the same error and link nothing', async ({ request }) => {
    const family = newFamily();
    await register(request, family);

    const wrongPhone = await join(request, family.mother, family.studentCode, '0999999999');
    const unknownCode = await join(request, family.mother, `nope-${family.tag}`, family.studentPhone);
    expect(wrongPhone.status()).toBe(404);
    expect(unknownCode.status()).toBe(404);
    expect(await wrongPhone.text()).toBe(await unknownCode.text());

    const vote = await (await request.get('/api/venue-vote', { headers: family.mother })).json();
    expect(vote.isRegistered).toBe(false);
  });

  test('an empty phone never matches a family with no phone on file', async ({ request }) => {
    const family = newFamily();
    await register(request, family, family.father, family.payload({ student: { studentId: family.studentCode, firstName: 'เด็ก', lastName: 'x', phone: '', newRoom: '1/2', newNo: 6 } }));
    const response = await join(request, family.mother, family.studentCode, '');
    expect(response.status()).toBe(400);
  });

  test('a LINE account that is already registered cannot join another student', async ({ request }) => {
    const family = newFamily();
    const other = newFamily();
    await register(request, family);
    await register(request, other);

    const response = await join(request, family.father, other.studentCode, other.studentPhone);
    expect(response.status()).toBe(409);
  });

  test('a secondary guardian row that already has LINE cannot be taken over', async ({ request }) => {
    const family = newFamily();
    await register(request, family);
    expect((await join(request, family.mother, family.studentCode, family.studentPhone)).status()).toBe(200);

    const stranger = as(`e2e-stranger-${family.tag}`);
    const response = await join(request, stranger, family.studentCode, family.studentPhone);
    expect(response.status()).toBe(409);

    const mother = await request.get('/api/me', { headers: family.mother });
    expect(mother.status()).toBe(200);
  });

  test("editing the registration as the mother saves each parent's own row and keeps both LINE links", async ({ request }) => {
    const family = newFamily();
    await register(request, family);
    expect((await join(request, family.mother, family.studentCode, family.studentPhone)).status()).toBe(200);

    // Mother opens "edit profile" and saves: the form re-sends both guardians, with changes to both.
    const profile = await (await request.get('/api/me', { headers: family.mother })).json();
    const edited = {
      student: { ...family.payload().student, nickname: 'น้อง' },
      guardians: profile.guardians.map((g) => ({
        order: g.order, relationType: g.relationType, firstName: g.order === 1 ? 'พ่อใหม่' : 'แม่ใหม่', lastName: g.lastName, phone: g.phone,
      })),
    };
    await register(request, family, family.mother, edited);

    for (const headers of [family.father, family.mother]) {
      const response = await request.get('/api/me', { headers });
      expect(response.status(), await response.text()).toBe(200);
      const body = await response.json();
      expect(body.student.nickname).toBe('น้อง');
      expect(body.guardians.map((g) => [g.order, g.firstName])).toEqual([[1, 'พ่อใหม่'], [2, 'แม่ใหม่']]);
    }

    const father = await (await request.get('/api/me', { headers: family.father })).json();
    expect(father.guardians.find((g) => g.order === 1).lineUserId).toBe(`e2e-father-${family.tag}`);
    expect(father.guardians.find((g) => g.order === 2).lineUserId).toBe(`e2e-mother-${family.tag}`);
  });

  test('too many wrong attempts are throttled with 429, and a right answer is blocked too', async ({ request }) => {
    const family = newFamily();
    await register(request, family);

    for (let i = 0; i < 5; i += 1) {
      const miss = await join(request, family.mother, family.studentCode, '0999999999');
      expect(miss.status()).toBe(404);
    }
    const blocked = await join(request, family.mother, family.studentCode, family.studentPhone);
    expect(blocked.status()).toBe(429);

    // Another LINE account is not affected.
    const other = await join(request, as(`e2e-other-${family.tag}`), family.studentCode, family.studentPhone);
    expect(other.status()).toBe(200);
  });
});
