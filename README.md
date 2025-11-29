Основан на https://github.com/iphelix/dnschef переписан на C# 

Получить статус сервера:
GET /api/dnsserver/status

Запустить сервер:
POST /api/dnsserver/start

Получить все маппинги:
GET /api/dnsmappings

Добавить маппинг:
POST /api/dnsmappings
Content-Type: application/json

{
  "domain": "example.com",
  "ipAddress": "127.0.0.1"
}

DELETE /api/dnsmappings/example.com


RESTful API - полное управление через HTTP endpoints

Потокобезопасность - блокировки для работы с коллекциями

Логирование - детальное логирование операций
