using System;
using System.Collections.Generic;
using System.Text;

namespace xNetStandart
{
    public class JsonContent : StringContent
    {
        public JsonContent(string content) : base(content)
        {
            _contentType = "application/json";
        }
    }
}
