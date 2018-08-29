using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json.Linq;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Newtonsoft.Json;

namespace recommendations_api.Controllers
{
    [ApiController]
    public class UsersController : Controller
    {
        GremlinServer gremlinServer;
        public UsersController(IDatabase db)
        {
            this.gremlinServer = db.GetGremlinServer();
        }

        [Route("api/createuser")]
        [HttpPost]
        public IActionResult CreateUser([FromBody] dynamic value)
        {
            string firstName = ((string) value.firstName).Replace("'", @"\'");
            string lastName = ((string) value.lastName).Replace("'", @"\'");
            string username = ((string) value.username).Replace("'", @"\'");
            string password = HashPassword(((string) value.password).Replace("'", @"\'"));
            string accessToken = Guid.NewGuid().ToString();
            string query = $"g.V().hasLabel('user').has('username', '{username}');";
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                if (task.Result.Count != 0)
                {
                    return StatusCode(403);
                }
                query = $"g.addV('user').property('firstName', '{firstName}').property('lastName', '{lastName}').property('username', '{username}').property('password', '{password}').property('accessToken', '{accessToken}');";
                task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait(); 
            }
            return Json(new { accessToken = accessToken });
        }

        [Route("api/signin")]
        [HttpPost]
        public IActionResult SignIn([FromBody] dynamic value)
        {
            string username = ((string)value.username).Replace("'", @"\'");
            string password = HashPassword(((string)value.password).Replace("'", @"\'"));
            string accessToken = Guid.NewGuid().ToString();
            string query = $"g.V().hasLabel('user').has('username', '{username}').has('password', '{password}').property('accessToken', '{accessToken}');";
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var task = gremlinClient.SubmitAsync<dynamic>(query);
                task.Wait();
                if (task.Result.Count == 1)
                {
                    return Json(new { accessToken = accessToken });
                }
                else if (task.Result.Count == 0)
                {
                    return StatusCode(401);
                }
                else
                {
                    return StatusCode(500);
                }
            }
        }

        [Route("api/signout")]
        [HttpPost]
        public IActionResult SignOut([FromBody] dynamic value)
        {
            string accessToken = Guid.NewGuid().ToString();
            string query = $"g.V().hasLabel('user').has('accessToken', {accessToken}).property('accessToken', '');";
            return StatusCode(200);
        }

        private string HashPassword(string password)
        {
            JObject o1 = JObject.Parse(System.IO.File.ReadAllText(@"salt.json"));
            int saltLocation = (int)o1["salt_location"];
            string salt = (string)o1["salt"];
            string saltedPassword = password.Substring(0, saltLocation) + salt + password.Substring(saltLocation);
            StringBuilder sb = new StringBuilder();
            using (var hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(saltedPassword));

                foreach (Byte b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            return sb.ToString();
        }
    }
}