using System;
using System.Net.Http;
using System.Runtime.Serialization;
using SimpleRankingsServer;

namespace CSharp
{
    class Program
    {
        [DataContract]
        class Sample1
        {
            [DataMember]
            public int Score1 { get; set; }
            [DataMember]
            public double Score2 { get; set; }
            [DataMember]
            public string Name { get; set; }

            public override string ToString()
            {
                return $"{{ \"Score1\" : {Score1}, \"Score2\" : {Score2}, \"Name\" : {Name} }}";
            }
        }

        const string Url = @"http://localhost:8080/v1/Sample1";
        const string Username = "sample";
        const string Password = "sample";

        static void Main(string[] args)
        {
            // clientは使い回す
            Client client = new Client(Url, Username, Password);

            // PlayerのGuidはファイルなどに保存しておく
            var userId = Guid.NewGuid();

            // insertするデータを作成
            var sample = new Sample1 { Score1 = 118, Score2 = 204.6, Name = "kitsune" };

            // データベースに追加
            // 追加したデータのidを取得
            var result = client.Insert(userId, sample).Result;
            Console.WriteLine(result);

            // データベースから取得
            var data = client.Select<Sample1>(orderBy: "Id", limit: 2).Result;
            foreach (var x in data)
            {
                Console.WriteLine(x);
            }

            Console.ReadLine();
        }
    }
}
