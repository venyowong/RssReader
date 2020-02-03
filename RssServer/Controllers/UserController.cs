using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RssServer.Models;
using RssServer.User.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RssServer.Controllers
{
    [Route("user")]
    public class UserController : Controller
    {
        private IUserService userService;

        private ILogger<UserController> logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            this.userService = userService;
            this.logger = logger;
        }

        [HttpPost, Route("login"), ModelValidation]
        public object Login([Required]string mail, [Required]string password)
        {
            if (this.userService == null)
            {
                return new ResultModel
                {
                    Code = 2,
                    Message = "UserService is missing."
                };
            }

            (int Code, UserEntity Data) loginResult = this.userService.Login(mail, password);
            if (loginResult.Code == -1)
            {
                return new ResultModel
                {
                    Code = 1
                };
            }

            if (loginResult.Code == 1)
            {
                return new ResultModel
                {
                    Code = 1,
                    Message = "密码错误"
                };
            }

            if (loginResult.Code != 0)
            {
                this.logger?.LogError($"mail: {mail}, password: {password}, login code: {loginResult.Code}");
                return new ResultModel
                {
                    Code = 2
                };
            }

            return new ResultModel<UserEntity>
            {
                Code = 0,
                Data = loginResult.Data
            };
        }
    }
}
