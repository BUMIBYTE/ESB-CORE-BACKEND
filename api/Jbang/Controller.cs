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

        [HttpGet("folders")]
        public IActionResult GetRootFolders()
        {
            try
            {
                var data = _service.ReadRootFolders();
                return Ok(ResponseHelper.Success(data, "List root folder"));
            }
            catch (Exception ex)
            {
                return BadRequest(ResponseHelper.Error(ex.Message));
            }
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

        [HttpPut("file")]
        public IActionResult UpdateFile([FromQuery] string path, [FromBody] string content)
        {
            try
            {
                var result = _service.UpdateFile(path, content);
                return Ok(ResponseHelper.Success(result, "Update file"));
            }
            catch (Exception ex)
            {
                return BadRequest(ResponseHelper.Error(ex.Message));
            }
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

        [HttpPost("run")]
        public IActionResult Run(string path, int? port)
        {
            try
            {
                var jobId = _service.RunJbang(path, port);

                return Ok(ResponseHelper.Success(jobId, "Jbang started"));
            }
            catch (Exception ex)
            {
                return BadRequest(ResponseHelper.Error(ex.Message));
            }
        }

        [HttpGet("status")]
        public IActionResult Status(string jobId)
        {
            var job = _service.GetStatus(jobId);

            if (job == null)
                return NotFound(ResponseHelper.Error("Job tidak ditemukan"));

            return Ok(ResponseHelper.Success(job, "Status job"));
        }

        [HttpPost("stop")]
        public IActionResult Stop(string jobId)
        {
            var result = _service.StopJob(jobId);

            if (result.Contains("tidak ditemukan"))
                return NotFound(ResponseHelper.Error(result));

            return Ok(ResponseHelper.Success(result, "Job stopped"));
        }

        [HttpGet("jobs")]
        public IActionResult GetAllJobs()
        {
            var jobs = _service.GetAllJobs();

            return Ok(ResponseHelper.Success(jobs, "List semua job"));
        }

        [HttpPost("resume")]
        public IActionResult Resume(string jobId)
        {
            var result = _service.ResumeJob(jobId);

            return Ok(ResponseHelper.Success(result, "Job berhasil dijalankan ulang"));
        }
    }
}
