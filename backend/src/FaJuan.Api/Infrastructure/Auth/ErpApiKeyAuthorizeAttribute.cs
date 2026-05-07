using Microsoft.AspNetCore.Mvc;

namespace FaJuan.Api.Infrastructure.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ErpApiKeyAuthorizeAttribute() : TypeFilterAttribute(typeof(ErpApiKeyAuthorizeFilter));
