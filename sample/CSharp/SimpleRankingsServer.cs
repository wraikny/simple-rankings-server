using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SimpleRankingsServer
{
    [DataContract]
    public class Data<T>
    {
        [DataMember]
        private string userId { get; set; }

        [DataMember]
        private string utcDate { get; set; }


        [DataMember(Name = "id")]
        public long Id { get; private set; }


        [DataMember(Name = "values")]
        public T Values { get; private set; }

        [IgnoreDataMember]
        public Guid UserId { get; private set; }

        [IgnoreDataMember]
        public DateTime UTCDate { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext s)
        {
            UserId = Guid.Parse(userId);
            UTCDate = DateTime.ParseExact(utcDate, "yyyy/MM/dd HH:mm:ss", null);
        }

        public override string ToString()
        {
            return $"{{ \"id\" : {Id}, \"userId\" : {userId}, \"utcDate\" : {utcDate}, \"values\" : {Values} }}";
        }
    }

    public static class JsonUtils
    {
        public static string Serialize<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static T Deserialize<T>(string json)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(ms);
            }
        }
    }

    public class Client : IDisposable
    {
        private readonly string url;
        private readonly HttpClient client = new HttpClient();

        public Client(string url, string username, string password)
        {
            this.url = url;
            var parameter = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", parameter);
        }

        public void Dispose()
        {
            client.Dispose();
        }

        [DataContract]
        private class InsertParam<T>
        {
            [DataMember(Name = "table")]
            public string Table { get; set; }

            [DataMember(Name = "userId")]
            public string UserIdStr { get; set; }

            [DataMember(Name = "values")]
            public T Values { get; set; }

            [IgnoreDataMember]
            private Guid userId;

            [IgnoreDataMember]
            public Guid UserId
            {
                get => userId;
                set
                {
                    userId = value;
                    UserIdStr = value.ToString();
                }
            }
        }

        [DataContract]
        private class InsertResult
        {
            [DataMember(Name = "id")]
            public long Id { get; set; }
        }

        public async Task<long> InsertAsync<T>(string tableName, Guid userId, T data)
        {
            var json = JsonUtils.Serialize(new InsertParam<T> { Table = tableName, UserId = userId, Values = data });
            using (var content = new StringContent(json, Encoding.UTF8, @"application/json"))
            {
                var result = await client.PostAsync(url, content);
                var resString = await result.Content.ReadAsStringAsync();

                if (result.IsSuccessStatusCode)
                {
                    return JsonUtils.Deserialize<InsertResult>(resString).Id;
                }

                throw new Exception($"{result.StatusCode}:{resString}");
            }
        }

        public async Task<IReadOnlyList<Data<T>>> SelectAsync<T>(string table, string orderBy = null, bool isDescending = true, int limit = 100)
        {
            var param = new Dictionary<string, string>();
            param.Add("table", table);
            param.Add("isDescending", isDescending.ToString());
            param.Add("limit", limit.ToString());
            if (orderBy != null) param.Add("orderBy", orderBy);

            var result = await client.GetAsync($"{url}?{await new FormUrlEncodedContent(param).ReadAsStringAsync()}");
            var resString = await result.Content.ReadAsStringAsync();

            if (result.IsSuccessStatusCode)
            {
                return JsonUtils.Deserialize<Data<T>[]>(resString);
            }

            throw new Exception($"{result.StatusCode}:{resString}");
        }
    }
}