using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jabil.Lib.Common
{
    public class ApiResponse
    {
        public string Message { get; set; }

        public bool Status { get; set; }

        public object Result { get; set; }

        public ApiResponse(string message = null, bool status = false, object result = default)
        {
            Message = message;
            Status = status;
            Result = result;
        }


    }

    public class ApiResponse<T>
    {
        public string Message { get; set; }

        public bool Status { get; set; }

        public T Result { get; set; }

        public ApiResponse(string message = null, bool status = false, T result = default)
        {
            Message = message;
            Status = status;
            Result = result;
        }
    }
}
