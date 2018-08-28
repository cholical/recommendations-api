using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Cryptography.Algorithms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace recommendations_api.Controllers
{
    [ApiController]
    public class UsersController : Controller
    {
        [Route("api/createuser")]
        [HttpPost]
        public IActionResult CreateUser([FromBody] dynamic value)
        {
            string firstName = (string) value.firstName;
            string lastName = (string) value.lastName;
            string username = (string) value.username;
            string password = (string) value.password;

            return Json(new { name = "Ronald Ronald Ronald" });
        }

        [Route("api/signin")]
        public IActionResult SignIn([FromBody] dynamic value)
        {
            return Json(new { });
        }
        [Route("api/signout")]
        public IActionResult SignOut([FromBody] dynamic value)
        {
            return Json(new { });
        }
    }
}