using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryPattern.Services.JbangService;
using System.Text.Json;
using System.Threading.Tasks;

namespace Beres.Server.Controllers
{
    [ApiController]
    [Route("api/v1/jbang")]
    public class JbangController : ControllerBase
    {
        private readonly IJbangService _service;

        public JbangController(IJbangService service)
        {
            _service = service;
        }

        [HttpPost("folder")]
        public IActionResult CreateFolder(string name)
        {
            var result = _service.CreateFolder(name);

            return Ok(ResponseHelper.Success(result, "Folder berhasil dibuat"));
        }

        [HttpPost("file")]
        public IActionResult CreateFile(string folder, string fileName)
        {
            var result = _service.CreateFile(folder, fileName, "Hello dari .NET");

            return Ok(ResponseHelper.Success(result, "File berhasil dibuat"));
        }

        [HttpGet("read")]
        public IActionResult ReadFile(string path)
        {
            var result = _service.ReadFile(path);

            return Ok(ResponseHelper.Success(result, "Berhasil membaca file"));
        }

        [HttpGet("folder")]
        public IActionResult ReadFolder(string path = "")
        {
            var result = _service.ReadFolder(path);

            return Ok(ResponseHelper.Success(result, "Berhasil membaca folder"));
        }

        [HttpDelete("file")]
        public IActionResult DeleteFile([FromQuery] string path)
        {
            var result = _service.DeleteFile(path);

            if (result.Contains("tidak ditemukan"))
                return BadRequest(ResponseHelper.Error(result));

            return Ok(ResponseHelper.Success(result, "File berhasil dihapus"));
        }

        [HttpDelete("folder")]
        public IActionResult DeleteFolder([FromQuery] string path)
        {
            var result = _service.DeleteFolder(path);

            if (result.Contains("tidak kosong") || result.Contains("tidak ditemukan"))
                return BadRequest(ResponseHelper.Error(result));

            return Ok(ResponseHelper.Success(result, "Folder berhasil dihapus"));
        }
    }
}
