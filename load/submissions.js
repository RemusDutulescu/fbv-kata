// A steady stream of new orders for the dashboard: RATE submissions a second for DURATION.
// docker compose run --rm -e RATE=50 -e DURATION=5m k6
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  scenarios: {
    submissions: {
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.RATE || 20),
      timeUnit: '1s',
      duration: __ENV.DURATION || '2m',
      preAllocatedVUs: 50,
      maxVUs: 500,
    },
  },
};

export default function () {
  const id = crypto.randomUUID();
  const order = {
    id,
    customerId: `k6-${id}`,
    total: 48.5,
    items: [
      { type: 'book', title: 'The Girl on the train' },
      { type: 'membership', membership: 'bookClub' },
    ],
  };

  // k6 sends no Content-Type for a string body, and the API would answer 415.
  const response = http.post(`${__ENV.BASE_URL}/v1/purchase-orders`, JSON.stringify(order), {
    headers: { 'Content-Type': 'application/json' },
  });
  check(response, { 'accepted (201 or 202)': (r) => r.status === 201 || r.status === 202 });
}
