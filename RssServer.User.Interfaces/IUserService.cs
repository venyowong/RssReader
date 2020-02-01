using System;
using System.Collections.Generic;
using System.Text;

namespace RssServer.User.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// 登录
        /// <para>-1 参数异常</para>
        /// <para>0 登录成功</para>
        /// <para>1 密码错误</para>
        /// <para>2 其他错误</para>
        /// </summary>
        /// <param name="mail"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        (int, UserEntity) Login(string mail, string password);

        /// <summary>
        /// 重置密码
        /// <para>-1 参数异常</para>
        /// <para>0 成功</para>
        /// <para>1 其他错误</para>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="oldPwd"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        int ResetPassword(string userId, string oldPwd, string password);
    }
}
