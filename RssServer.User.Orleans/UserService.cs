using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using RssServer.User.Interfaces;
using System;
using System.Text.Json;
using User.Interfaces;

namespace RssServer.User.Orleans
{
    public class UserService : IUserService, IDisposable
    {
        private IClusterClient client;

        public UserService(IConfiguration config)
        {
            this.client = new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = config["Orleans:ClusterId"];
                    options.ServiceId = config["Orleans:ServiceId"];
                })
                .UseAdoNetClustering(options =>
                {
                    options.Invariant = "MySql.Data.MySqlClient";
                    options.ConnectionString = config["Orleans:ConnectionString"];
                })
                .ConfigureApplicationParts(parts =>
                {
                    parts.AddApplicationPart(typeof(IUserGrain).Assembly)
                        .AddApplicationPart(typeof(ITokenGrain).Assembly)
                        .WithReferences();
                })
                .Build();
        }

        public void Dispose()
        {
            this.client?.Dispose();
        }

        public (int, Interfaces.UserEntity) Login(string mail, string password)
        {
            if (string.IsNullOrWhiteSpace(mail) || string.IsNullOrWhiteSpace(password))
            {
                return (-1, null);
            }

            var userGrain = this.client.GetGrain<IUserGrain>(mail);
            var loginResult = userGrain.Login(mail, password).Result;
            if (loginResult?.Code == 102)
            {
                return (1, null);
            }

            var userId = string.Empty;
            if (loginResult?.Code == 101)
            {
                var signUpResult = userGrain.SignUp(mail, password).Result;
                if (signUpResult.Code == 0)
                {
                    userId = signUpResult.Data;
                }
                else
                {
                    return (2, null);
                }
            }
            else if (loginResult?.Code != 0)
            {
                return (2, null);
            }
            else
            {
                userId = loginResult.Data?.Id;
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                return (2, null);
            }

            var user = new Interfaces.UserEntity
            {
                Id = userId,
                Mail = mail
            };
            var tokenGrain = this.client.GetGrain<ITokenGrain>(userId);
            var tokenResult = tokenGrain.Generate(JsonSerializer.Serialize(user)).Result;
            if (tokenResult.Code != 0 || string.IsNullOrWhiteSpace(tokenResult.Data))
            {
                return (2, null);
            }

            user.Token = tokenResult.Data;
            return (0, user);
        }

        public int ResetPassword(string userId, string oldPwd, string password)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(oldPwd))
            {
                return -1;
            }

            var userGrain = this.client.GetGrain<IUserGrain>(userId);
            var resetResult = userGrain.ResetPassword(userId, oldPwd, password).Result;
            return resetResult.Code == 0 ? 0 : 1;
        }
    }
}
