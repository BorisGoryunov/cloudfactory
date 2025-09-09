import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
    vus: 100,    
    stages: [
        { duration: "15s", target: 20 },
        { duration: "1m", target: 50 }, 
        { duration: "1m", target: 150 }, 
        { duration: "3s", target: 10 }, 
        { duration: "2s", target: 0 }, 
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
    //const url = `http://localhost:5211/api/Product${id}`;
    const url = `http://localhost:8080/api/Product${id}`;
    const res = http.get(url);

    // check(res, {
    //     'status is 200': (r) => r.status === 200,
    //     'response has products': (r) => r.json().length > 0,
    // });

    check(res, {
        "status is 200": (r) => r.status === 200,
    });

    if (res.status !== 200) {
        console.error(`❌ Ошибка: id=${id} status=${res.status}, body=${res.body}`);
    }    

    sleep(1);
}
