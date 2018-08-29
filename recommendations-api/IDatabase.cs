using Gremlin.Net.Driver;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace recommendations_api
{
    public interface IDatabase
    {
        string GetHostName();
        int GetPort();
        string GetAuthKey();
        string GetDatabase();
        string GetCollection();
        GremlinServer GetGremlinServer();
        DocumentClient GetDocumentClient();
    }
}
