using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gremlin.Net.Driver;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Diagnostics;

namespace recommendations_api.Controllers
{
    
    [ApiController]
    public class ProductController : Controller
    {
        private GremlinServer gremlinServer;
        private int viewedToViewed = 2;
        private int viewedToBought = 2;
        private int boughtToViewed = 2; //Everything that is bought is also viewed so affinity goes up by 4 total
        private int boughtToBought = 10;
        private int initialDepartmentMatch = 10;
        private int initialAisleMatch = 20; 
        public ProductController (IDatabase db)
        {
            this.gremlinServer = db.GetGremlinServer();
        }

        [Route("api/product")]
        [HttpPost]
        public IActionResult GetProduct([FromBody] dynamic value)
        {
            var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
            string productId = (string) value.productId;
            string userId;

            //Get productInfo
            string productQuery = $"g.V('{productId}');";
            var productTask = gremlinClient.SubmitAsync<dynamic>(productQuery);
            productTask.Wait();
            if (productTask.Result.Count != 1)
            {
                Debug.WriteLine("Status 500 thrown at Line 43");
                return StatusCode(500);
            }
            var product = productTask.Result.ElementAt(0);

            //Get UserId
            using (gremlinClient)
            {
                string query = $"g.V().hasLabel('user').has('accessToken', '{value.accessToken}');";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                if (task.Result.Count != 1)
                {
                    return StatusCode(403);
                }
                userId = (string) task.Result.ElementAt(0)["id"];
            }

            //Create "Viewed" edge from user to product
            using (gremlinClient)
            {
                string query = $"g.V('{userId}').outE('viewed').inV().has('id', '{productId}');";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                if (task.Result.Count == 0)
                {
                    query = $"g.V('{userId}').addE('viewed').to(g.V('{productId}'));";
                    task = gremlinClient.SubmitAsync<dynamic>(query);
                    task.Wait();
                }
            }

            //Update affinity with previous viewed
            using (gremlinClient)
            {
                string query = $"g.V('{userId}').outE('viewed').inV();";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                foreach (var result in task.Result)
                {
                    Debug.WriteLine("productId: " + (string) productId);
                    if (productId.Equals((string) result["id"]))
                    {
                        continue;
                    }
                    query = $"g.V('{productId}').bothE().where(outV().has('id', '{result["id"]}').or().inV().has('id', '{result["id"]}')).hasLabel('affinity');";
                    var subTask = gremlinClient.SubmitAsync<dynamic>(query);
                    subTask.Wait();
                    if (subTask.Result.Count == 1)
                    {
                        query = $"g.E('{subTask.Result.ElementAt(0)["id"]}').property('weight', {((int) subTask.Result.ElementAt(0)["properties"]["weight"]) + viewedToViewed});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else if (subTask.Result.Count == 0)
                    {
                        int weight = viewedToViewed;
                        query = $"g.V('{productId}').out('inDepartment').in('inDepartment').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialDepartmentMatch;
                        }
                        query = $"g.V('{productId}').out('inAisle').in('inAisle').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialAisleMatch;
                        }
                        query = $"g.V('{productId}').addE('affinity').to(g.V('{result["id"]}')).property('weight', {weight});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else
                    {
                        Debug.WriteLine("Status 500 thrown at 119");
                        return StatusCode(500);
                    }
                }
            }

            //Update affinity with previous bought
            using (gremlinClient)
            {
                string query = $"g.V('{userId}').outE('bought').inV();";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                foreach (var result in task.Result)
                {
                    if (productId.Equals((string) result["id"]))
                    {
                        continue;
                    }
                    query = $"g.V('{productId}').bothE().where(outV().has('id', '{result["id"]}').or().inV().has('id', '{result["id"]}')).hasLabel('affinity');";
                    var subTask = gremlinClient.SubmitAsync<dynamic>(query);
                    subTask.Wait();
                    if (subTask.Result.Count == 1)
                    {
                        query = $"g.E('{subTask.Result.ElementAt(0)["id"]}').property('weight', {((int) subTask.Result.ElementAt(0)["properties"]["weight"]) + viewedToBought});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else if (subTask.Result.Count == 0)
                    {
                        int weight = viewedToBought;
                        query = $"g.V('{productId}').out('inDepartment').in('inDepartment').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialDepartmentMatch;
                        }
                        query = $"g.V('{productId}').out('inAisle').in('inAisle').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialAisleMatch;
                        }
                        query = $"g.V('{productId}').addE('affinity').to(g.V('{result["id"]}')).property('weight', {weight});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else
                    {
                        return StatusCode(500);
                    }
                }
            }

            //Generate recommendations
            using (gremlinClient)
            {
                string query = $"g.V('{value.productId}').bothE('affinity').order().by('weight', decr).otherV().dedup();";
                var subTask = gremlinClient.SubmitAsync<dynamic>(query);
                subTask.Wait();
                return Json(new { product = product, recommendations = subTask.Result });
            }
        }

        [Route("api/buy")]
        [HttpPost]
        public IActionResult BuyProduct([FromBody] dynamic value)
        {
            var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
            string productId = (string) value.productId;
            string userId;

            //Get UserId
            using (gremlinClient)
            {
                string query = $"g.V().hasLabel('user').has('accessToken', '{value.accessToken}');";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                if (task.Result.Count != 1)
                {
                    return StatusCode(403);
                }
                userId = (string) task.Result.ElementAt(0)["id"];
            }

            //Create "bought" edge from user to product
            using (gremlinClient)
            {
                string query = $"g.V('{userId}').outE('bought').inV().has('id', '{productId}');";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                if (task.Result.Count == 0)
                {
                    query = $"g.V('{userId}').addE('bought').to(g.V('{productId}'));";
                    task = gremlinClient.SubmitAsync<dynamic>(query);
                    task.Wait();
                }
            }

            //Update affinity with previous viewed
            using (gremlinClient)
            {
                string query = $"g.V('{userId}').outE('viewed').inV();";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                foreach (var result in task.Result)
                {
                    if (productId.Equals((string) result["id"]))
                    {
                        continue;
                    }
                    query = $"g.V('{productId}').bothE().where(outV().has('id', '{result["id"]}').or().inV().has('id', '{result["id"]}')).hasLabel('affinity');";
                    var subTask = gremlinClient.SubmitAsync<dynamic>(query);
                    subTask.Wait();
                    if (subTask.Result.Count == 1)
                    {
                        query = $"g.E('{subTask.Result.ElementAt(0)["id"]}').property('weight', {((int)subTask.Result.ElementAt(0)["properties"]["weight"]) + boughtToViewed});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else if (subTask.Result.Count == 0)
                    {
                        int weight = boughtToViewed;
                        query = $"g.V('{productId}').out('inDepartment').in('inDepartment').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialDepartmentMatch;
                        }
                        query = $"g.V('{productId}').out('inAisle').in('inAisle').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialAisleMatch;
                        }
                        query = $"g.V('{productId}').addE('affinity').to(g.V('{result["id"]}')).property('weight', {weight});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else
                    {
                        return StatusCode(500);
                    }
                }
            }

            //Update affinity with previous bought
            using (gremlinClient)
            {
                string query = $"g.V('{userId}').outE('bought').inV();";
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                foreach (var result in task.Result)
                {
                    if (productId.Equals((string) result["id"]))
                    {
                        continue;
                    }
                    query = $"g.V('{productId}').bothE().where(outV().has('id', '{result["id"]}').or().inV().has('id', '{result["id"]}')).hasLabel('affinity');";
                    var subTask = gremlinClient.SubmitAsync<dynamic>(query);
                    subTask.Wait();
                    if (subTask.Result.Count == 1)
                    {
                        query = $"g.E('{subTask.Result.ElementAt(0)["id"]}').property('weight', {((int) subTask.Result.ElementAt(0)["properties"]["weight"]) + boughtToBought});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else if (subTask.Result.Count == 0)
                    {
                        int weight = boughtToBought;
                        query = $"g.V('{productId}').out('inDepartment').in('inDepartment').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialDepartmentMatch;
                        }
                        query = $"g.V('{productId}').out('inAisle').in('inAisle').has('id', '{result["id"]}');";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                        if (subTask.Result.Count != 0)
                        {
                            weight += initialAisleMatch;
                        }
                        query = $"g.V('{productId}').addE('affinity').to(g.V('{result["id"]}')).property('weight', {weight});";
                        subTask = gremlinClient.SubmitAsync<dynamic>(query);
                        subTask.Wait();
                    }
                    else
                    {
                        return StatusCode(500);
                    }
                }
            }
            return StatusCode(200);
        }
    }
}