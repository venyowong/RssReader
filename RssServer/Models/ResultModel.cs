using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RssServer.Models
{
    public class ResultModel
    {
        /// <summary>
        /// 0 正常 1 参数异常 2 系统异常
        /// </summary>
        public int Code { get; set; }

        public string Message { get; set; }
    }

    public class ResultModel<T> : ResultModel
    {
        public T Data { get; set; }
    }
}
