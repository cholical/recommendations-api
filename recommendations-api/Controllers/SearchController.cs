using System;
using System.Diagnostics;
using System.Linq;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace recommendations_api.Controllers
{
    [ApiController]
    public class SearchController : Controller
    {
        private Database db;
        private GremlinServer gremlinServer;
        private DocumentClient documentClient;
        public SearchController (IDatabase db)
        {
            this.db = (Database) db;
            this.gremlinServer = db.GetGremlinServer();
            this.documentClient = db.GetDocumentClient();
        }

        [Route("api/search")]
        [HttpGet]
        public IActionResult Search([FromQuery(Name = "keyword")] string keyword)
        {
            string query = $"SELECT * FROM c where(udf.REGEX(c.name[0]._value, '{keyword}') and c.label='product')";
            IQueryable<dynamic> results = documentClient.CreateDocumentQuery<dynamic>(UriFactory.CreateDocumentCollectionUri(db.GetDatabase(), db.GetCollection()), query);
            return Json(new { results });
        }
    }
}