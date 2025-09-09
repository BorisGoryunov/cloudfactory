#!/bin/bash
set -e

echo "🚀 Чистим систему от Docker Desktop..."
sudo apt remove --purge -y docker-desktop || true
rm -rf ~/.docker/desktop ~/.docker/features
rm -f ~/.config/systemd/user/docker-desktop.service

echo "✅ Docker Desktop удалён."

echo "🛠 Проверяем Docker Engine..."
sudo systemctl enable --now docker

echo "👤 Добавляем пользователя в группу docker..."
sudo usermod -aG docker $USER
newgrp docker <<EONG
echo "✅ Пользователь добавлен в группу docker."

echo "🔑 Логинимся в Docker Hub..."
docker login

echo "📦 Собираем тестовый образ..."
cat > Dockerfile <<EOF
FROM alpine:3.19
CMD ["echo", "Hello from native Docker Engine!"]
EOF

docker build -t $USER/test-image:1.0 .

echo "☁️ Пушим образ в Docker Hub..."
docker push $USER/test-image:1.0

echo "🧪 Тестируем: тянем образ с Docker Hub..."
docker run --rm $USER/test-image:1.0

echo "🎉 Готово! У тебя теперь чистый Docker Engine без Desktop."
EONG

docker context rm desktop-linux
