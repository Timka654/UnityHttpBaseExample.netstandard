using Appfox.Unity.AspNetCore.HTTP.Extensions.Examples;
using Appfox.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Appfox.Unity.AspNetCore.HTTP.Extensions
{
    internal class BaseWebRequestsExample : BaseWebRequests
    {
        public override string GetBaseDomain() => "http://localhosts:1010/";

        public const string SignInUrl = "User/SignIn";

        public static async void PasswordSignInSample3(PasswordSignInRequestModel query, Action<HttpRequestResult<string>> onResult)
        => await SafeInvoke<string>(Instance.SafeRequest<string>(SignInUrl, (message) => message.SetRequestBody(query)), result =>
        {
            if (result.MessageResponse.IsSuccessStatusCode)
                Instance.SetDefaultHeader("Authorization", $"Bearer { result.Data }");

            onResult(result);
        });

        public static async void PasswordSignInSample2(PasswordSignInRequestModel query, Action<HttpRequestResult<string>> onResult)
        {
            var result = await Instance.SafeRequest<string>(SignInUrl, (message) => message.SetRequestBody(query));

            if (result.MessageResponse.IsSuccessStatusCode)
                Instance.SetDefaultHeader("Authorization", $"Bearer { result.Data }");

            ThreadHelper.AddAction(() => onResult(result));
        }

        public static async void PasswordSignInSample1(PasswordSignInRequestModel query, Action<HttpRequestResult<string>> onResult)
        {
            var client = await Instance.GetClient();

            var result = await Instance.SafeRequest<string>(client, SignInUrl, (message) =>
            {
                message.SetRequestBody(query);
            });

            if (result.MessageResponse.IsSuccessStatusCode)
                Instance.SetDefaultHeader("Authorization", $"Bearer { result.Data }");

            ThreadHelper.AddAction(() => onResult(result));

            Instance.FreeClient(client, result);
        }

        private static BaseWebRequestsExample Instance;

        static BaseWebRequestsExample()
        {
            Instance = new BaseWebRequestsExample();
        }
    }
}
