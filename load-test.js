import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 100 },  // Ramp up to 100 users over 30s
    { duration: '1m', target: 100 },   // Stay at 100 users for 1m
    { duration: '30s', target: 500 },  // Ramp up to 500 users over 30s
    { duration: '2m', target: 500 },   // Stay at 500 users for 2m
    { duration: '30s', target: 1000 }, // Ramp up to 1000 users
    { duration: '2m', target: 1000 },  // Stay at 1000 users
    { duration: '30s', target: 0 },    // Ramp down to 0 users
  ],
  thresholds: {
    'http_req_duration': ['p(95)<2000'], // 95% of requests must complete below 2s
    'http_req_failed': ['rate<0.01'],     // Error rate must be below 1%
  },
};

const BASE_URL = 'http://localhost:8080/api';

export default function () {
  // Create user
  let createUserPayload = JSON.stringify({
    name: `User ${Math.random().toString(36).substring(7)}`,
    email: `user${Math.random().toString(36).substring(7)}@test.com`,
  });

  let createUserRes = http.post(`${BASE_URL}/users`, createUserPayload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(createUserRes, {
    'create user status is 201': (r) => r.status === 201,
    'create user response has ID': (r) => {
      try {
        return r.status === 201 && r.body && JSON.parse(r.body).id !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  let userId = null;
  if (createUserRes.status === 201 && createUserRes.body) {
    try {
      const parsed = JSON.parse(createUserRes.body);
      userId = parsed.id;
    } catch (e) {
      // Response is not valid JSON, skip
    }
  }

  if (userId) {
    // Get user by ID
    let getUserRes = http.get(`${BASE_URL}/users/${userId}`);
    check(getUserRes, { 'get user status is 200': (r) => r.status === 200 });

    // Get all users
    let getAllUsersRes = http.get(`${BASE_URL}/users`);
    check(getAllUsersRes, { 'get all users status is 200': (r) => r.status === 200 });

    // Create item for user
    let createItemPayload = JSON.stringify({
      userId: userId,
      title: `Task ${Math.random().toString(36).substring(7)}`,
      description: `Description ${Math.random().toString(36).substring(7)}`,
    });

    let createItemRes = http.post(`${BASE_URL}/items`, createItemPayload, {
      headers: { 'Content-Type': 'application/json' },
    });

    if (createItemRes.status === 201 && createItemRes.body) {
      try {
        const parsed = JSON.parse(createItemRes.body);
        let itemId = parsed.id;

        if (itemId) {
          // Get item by ID
          http.get(`${BASE_URL}/items/${itemId}`);

          // Get user's items
          http.get(`${BASE_URL}/items/user/${userId}`);
        }
      } catch (e) {
        // Response is not valid JSON, skip
      }
    }
  }

  sleep(1);
}

