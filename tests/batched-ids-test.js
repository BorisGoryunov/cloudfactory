import http from 'k6/http';
import { check } from 'k6';


const TOTAL_REQUESTS = 10000;     // Всего запросов
const IDS_COUNT = 100;            // Сколько уникальных ID
const BATCH_SIZE = TOTAL_REQUESTS / IDS_COUNT; // Запросов на один ID

export const options = {
  thresholds: {
    http_req_duration: ['p(95) < 500'],
    http_req_failed: ['rate < 0.01'],
  },

};

export default function () {

  const myId = (__VU - 1) % IDS_COUNT + 1; // ID от 1 до 100

  for (let i = 0; i < BATCH_SIZE; i++) {
    
    const url = `http://localhost:5211/api/Product/${myId}`;
    const res = http.get(url);

    check(res, {
      'status is 200': (r) => r.status === 200,
    });

  }
}