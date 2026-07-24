using System;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        public event Action<string, string, string>? OnNotificationRaised; // type, title, message
        public Func<string, string, bool>? ConfirmationHandler { get; set; }

        public void ShowSuccess(string title, string message)
        {
            OnNotificationRaised?.Invoke("Success", title, message);
            System.Diagnostics.Debug.WriteLine($"SUCCESS: [{title}] {message}");
        }

        public void ShowError(string title, string message)
        {
            OnNotificationRaised?.Invoke("Error", title, message);
            System.Diagnostics.Debug.WriteLine($"ERROR: [{title}] {message}");
        }

        public void ShowWarning(string title, string message)
        {
            OnNotificationRaised?.Invoke("Warning", title, message);
            System.Diagnostics.Debug.WriteLine($"WARNING: [{title}] {message}");
        }

        public bool ShowConfirmation(string title, string message)
        {
            if (ConfirmationHandler != null)
            {
                return ConfirmationHandler(title, message);
            }
            System.Diagnostics.Debug.WriteLine($"CONFIRMATION REQUIRED: [{title}] {message}");
            return true; // Default auto-approve in head-less scenario
        }
    }
}
