import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const acceptedReservations = new Counter('reservations_accepted');
const confirmedReservations = new Counter('reservations_confirmed');
const rejectedReservations = new Counter('reservations_rejected');
const rateLimitedRequests = new Counter('reservation_requests_rate_limited');
const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const beneficiaryId = __ENV.BENEFICIARY_ID || 'f87d30ea-8806-4e18-b292-0e03a52178dd';

export const options = {
  scenarios: {
    beneficiary_quota_contention: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 50 },
        { duration: '30s', target: 100 },
        { duration: '10s', target: 0 },
      ],
      gracefulRampDown: '30s',
    },
  },
  thresholds: {
    reservations_confirmed: ['count<=20'],
  },
};

export default function () {
  const idempotencyKey = `k6-${__VU}-${__ITER}-${Date.now()}`;
  const created = http.post(
    `${baseUrl}/api/v1/reservations`,
    JSON.stringify({
      beneficiaryId,
      benefitType: 'Consultation',
      amount: 25,
      currency: 'BRL',
    }),
    {
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': idempotencyKey,
      },
      redirects: 0,
    },
  );

  if (created.status === 429) {
    rateLimitedRequests.add(1);
    check(created, { 'rate limiter rejects excess load with 429': (response) => response.status === 429 });
    sleep(0.1);
    return;
  }

  const accepted = check(created, {
    'reservation is accepted asynchronously': (response) => response.status === 202,
  });
  if (!accepted) {
    return;
  }

  acceptedReservations.add(1);
  const location = created.headers.Location;
  if (!location) {
    return;
  }

  for (let attempt = 0; attempt < 20; attempt += 1) {
    const status = http.get(new URL(location, baseUrl).toString());
    if (status.status === 200) {
      const reservation = status.json();
      if (reservation.status === 'Confirmed' || reservation.status === 2) {
        confirmedReservations.add(1);
        return;
      }
      if (reservation.status === 'Rejected' || reservation.status === 3) {
        rejectedReservations.add(1);
        return;
      }
    }
    sleep(0.25);
  }
}
