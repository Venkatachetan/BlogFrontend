namespace BlogFrontend.Services
{
    public class NotificationService
    {
        private List<NotificationItem> _notifications = new List<NotificationItem>();
        public event Action OnNotificationsChanged;

        public class NotificationItem
        {
            public string Id { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public IReadOnlyList<NotificationItem> Notifications => _notifications.AsReadOnly();

        public void AddNotification(string message)
        {
            var newNotification = new NotificationItem
            {
                Id = Guid.NewGuid().ToString(),
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            _notifications.Add(newNotification);

            if (_notifications.Count > 30)
            {
                _notifications.RemoveAt(0);
            }

            OnNotificationsChanged?.Invoke();
        }

        public void ClearNotifications()
        {
            _notifications.Clear();
            OnNotificationsChanged?.Invoke();
        }
    }
}