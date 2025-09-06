#npm install -g k6
sudo snap install k6


k6 run product-test.js

k6 run --out json=results.json product-test.js
