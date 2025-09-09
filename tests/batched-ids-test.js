import http from 'k6/http';
import { check, sleep } from 'k6';


const TOTAL_REQUESTS = 20000;     // Всего запросов
const IDS_COUNT = 1000;            // Сколько уникальных ID
const BATCH_SIZE = TOTAL_REQUESTS / IDS_COUNT; // Запросов на один ID

export const options = {
   vus: 100,         
   duration: '1m', 
    // stages: [
    //     { duration: "15s", target: 20 },
    //     { duration: "15s", target: 50 },
    //     { duration: "35s", target: 150 },
    //     { duration: "1m", target: 250 },
    //     { duration: "15s", target: 50 },
    //     { duration: "10s", target: 20 },
    //     { duration: "2s", target: 0 }
    //   ],
  thresholds: {
    http_req_duration: ['p(95) < 500'],
    http_req_failed: ['rate < 0.01'],
  },

};

export default function () {

  const myId = (__VU % IDS_COUNT) + 1;

  

  for (let i = 0; i < BATCH_SIZE; i++) {
    
    //const url = `http://localhost:5211/api/Product/${myId}`;
    const url = `http://localhost:8080/api/Product${myId}`;
    const res = http.get(url);

    check(res, {
      'status is 200': (r) => r.status === 200,
    });

  }

  sleep(1);
}