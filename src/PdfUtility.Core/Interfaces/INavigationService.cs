using System;

namespace PdfUtility.Core.Interfaces
{
    public interface INavigationService
    {
        event Action<string>? NavigationRequested;
        void NavigateTo(string pageName);
    }
}
