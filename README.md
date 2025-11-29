Основан на https://github.com/iphelix/dnschef переписан на C# 

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
