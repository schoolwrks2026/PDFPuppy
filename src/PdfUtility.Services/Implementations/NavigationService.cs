using System;
using PdfUtility.Core.Interfaces;

namespace PdfUtility.Services.Implementations
{
    public class NavigationService : INavigationService
    {
        public event Action<string>? NavigationRequested;

        public void NavigateTo(string pageName)
        {
            if (string.IsNullOrWhiteSpace(pageName)) return;
            NavigationRequested?.Invoke(pageName);
        }
    }
}
