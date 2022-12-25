using System.Globalization;

namespace LinkShorteningAssignment.WebApi.Exceptions
{
    public class AppException : Exception
    {
        public AppException() : base() { }

        public AppException(string message) : base(message) { }
    }
}
