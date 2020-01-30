using System;
using System.Runtime.Serialization;

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

        // clientは使い回す
        static SimpleRankingsServer.Client client = new SimpleRankingsServer.Client(Url, Username, Password);

        // PlayerのGuidはファイルなどに保存しておく
        static Guid userId = Guid.NewGuid();

        static void Main(string[] args)
        {
            // insertするデータを作成
            var sample = new Sample1 { Score1 = 118, Score2 = 204.6, Name = "kitsune" };

            // データベースに追加
            // 追加したデータのidを取得
            var result = client.InsertAsync(userId, sample).Result;
            Console.WriteLine(result);

            // データベースから取得
            var data = client.SelectAsync<Sample1>(orderBy: "Id", limit: 2).Result;
            foreach (var x in data)
            {
                Console.WriteLine(x);
            }

            Console.ReadLine();

            client.Dispose();
        }
    }
}
