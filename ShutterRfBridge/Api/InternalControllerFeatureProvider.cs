using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ShutterRfBridge.Api;

/// <summary>
///     Also finds this assembly's internal API controllers, so they and the types they use can stay internal; the
///     default provider only finds public ones.
/// </summary>
internal sealed class InternalControllerFeatureProvider : ControllerFeatureProvider
{
    protected override bool IsController(TypeInfo typeInfo) =>
        base.IsController(typeInfo) ||
        (typeInfo.Assembly == typeof(InternalControllerFeatureProvider).Assembly &&
         typeInfo is { IsClass: true, IsAbstract: false } &&
         typeInfo.IsDefined(typeof(ApiControllerAttribute)));
}
