using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Newtonsoft.Json;

namespace recommendations_api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //ResetDatabase();
            CreateWebHostBuilder(args).Build().Run();
        }

        private static void ResetDatabase()
        {
            DropDatabase();
            SetupDatabase();
        }

        private static void DropDatabase()
        {
            JObject o1 = JObject.Parse(File.ReadAllText(@"dbconfig.json"));
            string hostname = (string) o1["hostname"];
            int port = (int) o1["port"];
            string authKey = (string) o1["authKey"];
            string database = (string) o1["database"];
            string collection = (string) o1["collection"];

            var gremlinServer = new GremlinServer(hostname, port, enableSsl: true, username: "/dbs/" + database + "/colls/" + collection, password: authKey);
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var task = gremlinClient.SubmitAsync<dynamic>("g.V().hasLabel('department').drop();");
                task.Wait();
                task = gremlinClient.SubmitAsync<dynamic>("g.V().hasLabel('aisle').drop()");
                task.Wait();
                task = gremlinClient.SubmitAsync<dynamic>("g.V().hasLabel('product').drop()");
                task.Wait();
                task = gremlinClient.SubmitAsync<dynamic>("g.E().drop()");
                task.Wait();
            }
        }

        private static void SetupDatabase() {
            JObject o1 = JObject.Parse(File.ReadAllText(@"dbconfig.json"));
            string hostname = (string) o1["hostname"];
            int port = (int) o1["port"];
            string authKey = (string) o1["authKey"];
            string database = (string) o1["database"];
            string collection = (string)o1["collection"];

            var gremlinServer = new GremlinServer(hostname, port, enableSsl: true, username: "/dbs/" + database + "/colls/" + collection, password: authKey);
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                //Loading departments
                var csvReader = new StreamReader(File.OpenRead(@"departments.csv"));
                var line = csvReader.ReadLine();
                while (!csvReader.EndOfStream)
                {
                    line = csvReader.ReadLine();
                    var values = line.Split(',');
                    values[1] = values[1].Replace("'", @"\'");
                    var task = gremlinClient.SubmitAsync<dynamic>($"g.addV('department').property('name', '{values[1]}');");
                    task.Wait();
                }
                //Loading aisles
                csvReader = new StreamReader(File.OpenRead(@"aisles.csv"));
                line = csvReader.ReadLine();
                while (!csvReader.EndOfStream)
                {
                    line = csvReader.ReadLine();
                    var values = line.Split(',');
                    values[1] = values[1].Replace("'", @"\'");
                    var task = gremlinClient.SubmitAsync<dynamic>($"g.addV('aisle').property('name', '{values[1]}');");
                    task.Wait();
                }
                //Loading productsg.V().has('name', 'poultry counter');
                csvReader = new StreamReader(File.OpenRead(@"sample_products.csv"));
                line = csvReader.ReadLine();
                while (!csvReader.EndOfStream)
                {
                    line = csvReader.ReadLine();
                    var values = line.Split('|');
                    values[1] = values[1].Replace("'", @"\'");
                    Debug.WriteLine($"Value[1]: {values[1]} | Value[2]: {values[2]} | Value[3]: {values[3]}");
                    //.addE('inDepartment').to(g.V().hasLabel('department').has('name', '{values[2]}'))
                    //.addE('inAisle').to(g.V().hasLabel('aisle').has('name', '{values[3]}'))
                    var task = gremlinClient.SubmitAsync<dynamic>($"g.addV('product').property('name', '{values[1]}').addE('inDepartment').to(g.V().hasLabel('department').has('name', '{values[2]}'));");
                    task.Wait();
                    task = gremlinClient.SubmitAsync<dynamic>($"g.V().hasLabel('product').has('name', '{values[1]}').addE('inAisle').to(g.V().hasLabel('aisle').has('name', '{values[3]}'))");
                    task.Wait();
                }
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
