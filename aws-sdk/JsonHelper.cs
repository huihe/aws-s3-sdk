using Newtonsoft.Json;

namespace aws_sdk
{
    public static class JsonHelper
    {
        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
