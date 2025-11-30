using DnsChef.Models;

namespace DnsChef.Services
{
    public interface ILogService
    {
        void AddLog(LogEntry logEntry);
        LogResponse GetLogs(LogQuery query);
        void ClearLogs();
        int GetTotalLogCount();
    }

    public class LogService : ILogService
    {
        private readonly List<LogEntry> _logs = new();
        private readonly object _lock = new();
        private readonly int _maxLogEntries = 10000; // Максимум записей в памяти

        public void AddLog(LogEntry logEntry)
        {
            lock (_lock)
            {
                // Ограничиваем размер лога
                if (_logs.Count >= _maxLogEntries)
                {
                    _logs.RemoveAt(0); // Удаляем самую старую запись
                }

                _logs.Add(logEntry);
            }
        }

        public LogResponse GetLogs(LogQuery query)
        {
            lock (_lock)
            {
                var filteredLogs = _logs.AsEnumerable();

                // Фильтрация по уровню
                if (!string.IsNullOrEmpty(query.Level))
                {
                    filteredLogs = filteredLogs.Where(x =>
                        x.Level.Equals(query.Level, StringComparison.OrdinalIgnoreCase));
                }

                // Фильтрация по домену
                if (!string.IsNullOrEmpty(query.Domain))
                {
                    filteredLogs = filteredLogs.Where(x =>
                        x.Domain != null && x.Domain.Contains(query.Domain, StringComparison.OrdinalIgnoreCase));
                }

                // Фильтрация по действию
                if (!string.IsNullOrEmpty(query.Action))
                {
                    filteredLogs = filteredLogs.Where(x =>
                        x.Action != null && x.Action.Equals(query.Action, StringComparison.OrdinalIgnoreCase));
                }

                // Фильтрация по дате
                if (query.StartDate.HasValue)
                {
                    filteredLogs = filteredLogs.Where(x => x.Timestamp >= query.StartDate.Value);
                }

                if (query.EndDate.HasValue)
                {
                    filteredLogs = filteredLogs.Where(x => x.Timestamp <= query.EndDate.Value);
                }

                // Сортировка по времени (новые сначала)
                filteredLogs = filteredLogs.OrderByDescending(x => x.Timestamp);

                var totalCount = filteredLogs.Count();
                var page = query.Page ?? 1;
                var pageSize = query.PageSize ?? 50;

                // Пагинация
                var pagedLogs = filteredLogs
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new LogResponse
                {
                    Logs = pagedLogs,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        public int GetTotalLogCount()
        {
            lock (_lock)
            {
                return _logs.Count;
            }
        }
    }
}