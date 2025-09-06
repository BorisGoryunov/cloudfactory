import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
    stages: [
        { duration: "30s", target: 20 },
        { duration: "2s", target: 10000 }, 
        { duration: "30s", target: 10 }, 
        { duration: "20s", target: 0 }, 
    ],
    thresholds: {
        http_req_duration: ["p(95)<500"], // 95% запросов должны выполняться быстрее 500мс
        http_req_failed: ["rate<0.01"], // Менее 1% ошибок
    },
};

function getRandomInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

export default function () {
    const id = getRandomInt(1, 1000);
    const url = `http://localhost:5211/api/Product${id}`;
    const res = http.get(url);

    // check(res, {
    //     'status is 200': (r) => r.status === 200,
    //     'response has products': (r) => r.json().length > 0,
    // });

    check(res, {
        "status is 200": (r) => r.status === 200,
    });

    sleep(1);
}
