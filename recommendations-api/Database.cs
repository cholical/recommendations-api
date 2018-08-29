using Gremlin.Net.Driver;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace recommendations_api
{
    public sealed class Database : IDatabase
    {
        private static Database instance = null;
        private static readonly object padlock = new object();
        private string hostname;
        private int port;
        private string authKey;
        private string database;
        private string collection;
        private string URIEndpoint;
        private GremlinServer gremlinServer;
        private DocumentClient documentClient;

        public Database()
        {
            JObject o1 = JObject.Parse(File.ReadAllText(@"dbconfig.json"));
            this.hostname = (string) o1["hostname"];
            this.port = (int) o1["port"];
            this.authKey = (string) o1["authKey"];
            this.database = (string) o1["database"];
            this.collection = (string) o1["collection"];
            this.URIEndpoint = (string) o1["URIEndpoint"];
            this.gremlinServer = new GremlinServer(hostname, port, enableSsl: true, username: "/dbs/" + database + "/colls/" + collection, password: authKey);
            this.documentClient = new DocumentClient(new Uri(URIEndpoint), authKey);
        }

        public static Database Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Database();
                    }
                    return instance;
                }
            }
        }
        public string GetHostName()
        {
            return hostname;
        }
        public int GetPort()
        {
            return port;
        }
        public string GetAuthKey()
        {
            return authKey;
        }
        public string GetDatabase()
        {
            return database;
        }
        public string GetCollection()
        {
            return collection;
        }
        public GremlinServer GetGremlinServer()
        {
            return gremlinServer; 
        }

        public DocumentClient GetDocumentClient()
        {
            return documentClient;
        }
    }
}
