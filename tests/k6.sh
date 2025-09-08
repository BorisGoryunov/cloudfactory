#npm install -g k6
sudo snap install k6


k6 run product-test.js

k6 run batched-ids-test.js

k6 run --out json=results.json product-test.js


K6_OUT=influxdb=http://localhost:8086/k6 \ 
k6 run product-test.js
K6_OUT=influxdb=http://localhost:8086/k6 \ 
k6 run batched-ids-test.js

#dashboard 2587