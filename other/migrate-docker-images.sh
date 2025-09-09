#!/bin/bash

set -e  # Прервать при ошибке

BACKUP_DIR="$HOME/docker-backup/images"
mkdir -p "$BACKUP_DIR"

echo "🔄 [Шаг 1] Переключаемся на контекст 'default' для экспорта..."
docker context use default

echo "📦 [Шаг 2] Получаем список всех образов..."
#docker images --format "{{.Repository}}:{{.Tag}}"> /tmp/images_list.txt
docker images --format "{{.Repository}}:{{.Tag}}" | grep -v "<none>:<none>" > /tmp/images_list.txt
#docker images --format "{{.ID}}" --filter "dangling=true" > /tmp/images_none.txt

echo "💾 [Шаг 3] Экспортируем именованные образы..."
cd "$BACKUP_DIR"

while IFS= read -r image; do
    if [[ -n "$image" && "$image" != "REPOSITORY:TAG" ]]; then
        filename=$(echo "$image" | sed 's/[\/:]/_/g').tar
        echo " → Сохраняю $image как $filename"
        docker save "$image" > "$filename"
    fi
done < /tmp/images_list.txt

# echo "💾 [Шаг 4] Экспортируем образы <none>:<none> по ID..."
# while IFS= read -r image_id; do
#     if [[ -n "$image_id" ]]; then
#         filename="image_${image_id}.tar"
#         echo " → Сохраняю образ ID $image_id как $filename"
#         docker save "$image_id" > "$filename"
#     fi
# done < /tmp/images_none.txt

echo "🔄 [Шаг 5] Переключаемся на контекст 'desktop-linux'..."
docker context use desktop-linux

echo "📥 [Шаг 6] Загружаем все образы в desktop-linux..."
cd "$BACKUP_DIR"

for file in *.tar; do
    if [[ -f "$file" ]]; then
        echo " → Загружаю $file"
        docker load < "$file"
    fi
done

echo "✅ [ГОТОВО] Все образы успешно перенесены в контекст 'desktop-linux'."

echo "📋 Проверь:"
echo "  docker context use desktop-linux"
echo "  docker images"
echo "  docker ps -a   # контейнеры нужно пересоздать вручную"

# Очистка временных файлов
rm -f /tmp/images_list.txt /tmp/images_none.txt

#docker context use default
#docker system prune -a -f

docker context use desktop-linux

echo "💡 Совет: Чтобы пересоздать контейнеры — используй 'docker run' с теми же параметрами, что и раньше."
echo "   Или восстанови через docker-compose, если использовал его."

#$ sudo sysctl -w kernel.apparmor_restrict_unprivileged_userns=0
#$ systemctl --user restart docker-desktop