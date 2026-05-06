using System;
using System.Text;

namespace Keyfactor.Extensions.Orchestrators.FortiManager
{
    internal static class ExceptionHandler
    {
        internal static string FlattenException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            Exception current = ex;
            while (current != null)
            {
                if (sb.Length > 0)
                    sb.Append(" --> ");
                sb.Append(current.Message);
                current = current.InnerException;
            }
            return sb.ToString();
        }
    }
}
