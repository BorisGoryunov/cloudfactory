#!/bin/bash

BACKUP_DIR="$HOME/docker-backup/images"

cd "$BACKUP_DIR"

for file in *.tar; do
    if [[ -f "$file" ]]; then
        echo " → Загружаю $file"
        docker load < "$file"
    fi
done

cd ..

echo "✅ OK"
