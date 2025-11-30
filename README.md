Как идея взят с https://github.com/iphelix/dnschef 

RESTful API - полное управление через HTTP endpoints

Потокобезопасность - блокировки для работы с коллекциями

Логирование - детальное логирование операций


### <ins>Получить статус сервера:</ins>
`GET`
```/api/dnsserver/status```

### <ins>Запустить сервер:</ins>
`POST`
```/api/dnsserver/start```

### <ins>Получить все маппинги:</ins>
`GET`
```/api/dnsmappings```

### <ins>Добавить маппинг:</ins>
`POST` 
```/api/dnsmappings```
```
Content-Type: application/json
{
  "domain": "example.com",
  "ipAddress": "127.0.0.1"
}
```
### <ins>Удалить маппинг:</ins>
`DELETE` ```/api/dnsmappings/example.com```



# Сделать скрипты исполняемыми
```chmod +x install-dnschef.sh build-dnschef.sh uninstall-dnschef.sh```

# 1. Собрать приложение (на машине разработки)
```./build-dnschef.sh```

# 2. Скопировать папку publish на Debian сервер
```scp -r publish/ user@debian-server:/tmp/dnschef/```

# 3. На Debian сервере:
```cd /tmp/dnschef
sudo ./install-dnschef.sh```

# 4. Проверить статус
```sudo systemctl status dnschef
journalctl -u dnschef -f```
