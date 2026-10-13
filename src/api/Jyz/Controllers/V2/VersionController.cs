using Asp.Versioning;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Jyz.Controllers.V2
{
    [ApiVersion("2.0")]
    public class VersionController : CustomController
    {
        [HttpGet]
        public IActionResult Get()
        {
            var assembly = typeof(VersionController).Assembly;
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            // Strip optional SemVer build metadata (e.g. "+abc123") so `version` is a clean x.y.z.
            var version = informationalVersion?.Split('+')[0];
            var assemblyVersion = assembly.GetName().Version?.ToString();
            var fileVersion = assembly
                .GetCustomAttribute<AssemblyFileVersionAttribute>()?
                .Version;

            return Ok(new
            {
                apiVersion = 2.0,
                name = assembly.GetName().Name,
                version = version ?? assemblyVersion,
                informationalVersion = informationalVersion,
                assemblyVersion = assemblyVersion,
                fileVersion = fileVersion,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            });
        }
    }
}
