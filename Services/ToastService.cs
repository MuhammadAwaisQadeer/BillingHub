using Radzen;

namespace Client_Invoice_System.Services
{
    public class ToastService
    {
        private readonly NotificationService _notificationService;

        public ToastService(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public void Notify(string type, string message)
        {
            var severity = type.ToLower() switch
            {
                "success" => NotificationSeverity.Success,
                "info" => NotificationSeverity.Info,
                "warning" => NotificationSeverity.Warning,
                "error" => NotificationSeverity.Error,
                _ => NotificationSeverity.Info
            };

            _notificationService.Notify(new NotificationMessage
            {
                Severity = severity,
                Summary = type.ToUpper(),
                Detail = message,
                Duration = 5000
            });
        }
    }
}