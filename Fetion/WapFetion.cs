﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NewLife.Exceptions;
using NewLife.Web;
using System.Net;

namespace NewLife.Net.Fetion
{
    /// <summary>Wap飞信</summary>
    public class WapFetion : DisposeBase
    {
        #region 属性
        private String _Mobile;
        /// <summary>手机号码</summary>
        public String Mobile { get { return _Mobile; } set { _Mobile = value; } }

        private String _Password;
        /// <summary>密码</summary>
        public String Password { get { return _Password; } set { _Password = value; } }

        private Boolean hasLogined;

        private WebClientX _Client;
        /// <summary>客户端</summary>
        public WebClientX Client
        {
            get
            {
                if (_Client == null)
                {
                    var client = new WebClientX(false, true);
                    //client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    var result = client.DownloadString(server + "/im/");
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

                    _Client = client;
                }
                return _Client;
            }
            set { _Client = value; }
        }
        #endregion

        /// <summary>构造函数</summary>
        /// <param name="mobile">手机号码</param>
        /// <param name="password">密码</param>
        public WapFetion(String mobile, String password)
        {
            Mobile = mobile;
            Password = HttpUtility.UrlEncode(password);
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (hasLogined) Logout();
        }

        const String server = "http://f.10086.cn";
        String Post(String uri, String data)
        {
            //return Encoding.UTF8.GetString(Client.UploadData(server + uri, Encoding.UTF8.GetBytes(data)));
            var d = Encoding.UTF8.GetBytes(data);
            d = Client.UploadData(server + uri, d);
            var result = Encoding.UTF8.GetString(d);
            return result;
        }

        /// <summary>登陆</summary>
        /// <returns></returns>
        public String Login()
        {
            if (hasLogined) return null;
            hasLogined = true;

            String uri = "/im/login/inputpasssubmit1.action";
            var result = Post(uri, String.Format("m={0}&pass={1}&loginstatus=1", Mobile, Password));
            if (NetHelper.Debug) NetHelper.WriteLog(result);
            return result;
        }

        /// <summary>注销</summary>
        /// <returns></returns>
        public String Logout()
        {
            String uri = "/im/index/logoutsubmit.action";
            return Post(uri, "");
        }

        /// <summary>通过手机号，给自己会好友发送消息</summary>
        /// <param name="mobile">手机号</param>
        /// <param name="message">消息</param>
        public void Send(String mobile, String message)
        {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException("message");

            if (!hasLogined) Login();

            if (mobile == Mobile)
                ToMyself(message);
            else
            {
                String uid = GetUid(mobile);
                if (uid != null) ToUid(uid, message);
            }
        }

        /// <summary>获取用户UID</summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        protected String GetUid(String mobile)
        {
            String uri = "/im/index/searchOtherInfoList.action";
            String data = "searchText=" + mobile;

            String result = Post(uri, data);
            Match mc = Regex.Match(result, @"toinputMsg\.action\?touserid=(\d+)");
            if (!mc.Success) return null;

            return mc.Groups[1].Value;
        }

        /// <summary>发往目标UID</summary>
        /// <param name="uid"></param>
        /// <param name="message"></param>
        protected void ToUid(String uid, String message)
        {
            String uri = "/im/chat/sendMsg.action?touserid=" + uid;
            String data = "msg=" + HttpUtility.UrlEncode(message);
            String result = Post(uri, data);
            if (!result.Contains("成功")) throw new XException(result);
        }

        /// <summary>发送给自己</summary>
        /// <param name="message"></param>
        protected void ToMyself(String message)
        {
            String uri = "/im/user/sendMsgToMyselfs.action";
            String data = "msg=" + HttpUtility.UrlEncode(message);
            String result = Post(uri, data);
            if (!result.Contains("成功")) throw new XException(result);
        }
    }
}