using System;
using System.Threading.Tasks;
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
        }

        const string Url = @"http://localhost:8080/api/SampleDB";
        const string Username = "sample";
        const string Password = "sample";

        // clientは使い回す
        static SimpleRankingsServer.Client client = new SimpleRankingsServer.Client(Url, Username, Password);

        // PlayerのGuidはファイルなどに保存しておく
        static Guid userId = Guid.NewGuid();

        static async Task Main(string[] args)
        {
            // insertするデータを作成
            var sample = new Sample1 { Score1 = 118, Score2 = 204.6, Name = "kitsune" };

            // データベースに追加
            // 追加したデータのidを取得
            var result = await client.InsertAsync("SampleTable", userId, sample);
            Console.WriteLine(result);

            // データベースから取得
            var data = await client.SelectAsync<Sample1>("SampleTable", orderBy: "Id", limit: 2);
            foreach (var x in data)
            {
                Console.WriteLine(x);
            }

            Console.ReadLine();
        }
    }
}
