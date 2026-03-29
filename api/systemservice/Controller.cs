using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryPattern.Services.SystemService;
using System.Text.Json;
using System.Threading.Tasks;

namespace Beres.Server.Controllers
{
    // [Authorize]
    [ApiController]
    [Route("api/v1/primakom")]
    public class SystemController : ControllerBase
    {
        private readonly ISystemService _SystemService;
        private readonly ConvertJWT _ConvertJwt;

        public SystemController(ConvertJWT convert, ISystemService SystemService)
        {
            _SystemService = SystemService;
            _ConvertJwt = convert;
        }

        [HttpGet("cpu")]
        public async Task<IActionResult> Cpu()
        {
            var cpu = await _SystemService.GetCpuDetail();
            return Ok(ResponseHelper.Success(cpu, "CPU Usage"));
        }

        [HttpGet("memory")]
        public IActionResult Memory()
        {
            var memory = _SystemService.GetMemoryDetail();
            return Ok(ResponseHelper.Success(memory, "Memory Usage (MB)"));
        }

        [HttpGet("storage")]
        public IActionResult Storage()
        {
            var storage = _SystemService.GetStorageDetail();
            return Ok(ResponseHelper.Success(storage, "Storage Usage (MB)"));
        }

        [HttpGet("server")]
        public IActionResult Server()
        {
            var data = _SystemService.GetServerInfo();

            return Ok(ResponseHelper.Success(data, "Server Details"));
        }
    }
}
