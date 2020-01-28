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

        static void Main(string[] args)
        {
            const string Url = @"http://localhost:8080/v1/Sample1";

            var userId = Guid.NewGuid();

            var sample = new Sample1 { Score1 = 118, Score2 = 204.6, Name = "kitsune" };
            var result = Client.Insert(Url, userId.ToString(), sample).Result;

            Console.WriteLine(result);

            var data = Client.Select<Sample1>(Url, orderBy: "Id", limit: 5).Result;
            foreach (var x in data)
            {
                Console.WriteLine(x);
            }

            Console.ReadLine();
        }
    }
}
