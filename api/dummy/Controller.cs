using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryPattern.Services.DummyService;
using System.Text.Json;
using System.Threading.Tasks;

namespace Beres.Server.Controllers
{
    // [Authorize]
    [ApiController]
    [Route("api/v1/dummy")]
    public class DummyController : ControllerBase
    {
        private readonly IDummyService _DummyService;
        private readonly ConvertJWT _ConvertJwt;

        public DummyController(ConvertJWT convert, IDummyService DummyService)
        {
            _DummyService = DummyService;
            _ConvertJwt = convert;
        }


        [HttpGet("sharepoint")]
        public async Task<IActionResult> GetDummyWA()
        {
            try
            {
                var result = await _DummyService.GetData();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("updatesharepoint")]
        public async Task<IActionResult> PatchDummyWA([FromBody] PushAssetModel request)
        {
            try
            {
                var result = await _DummyService.PatchDummyWA(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("sapecc")]
        public async Task<IActionResult> Getsapecc()
        {
            try
            {
                var result = await _DummyService.GetDataECC();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("pushtosap")]
        public async Task<IActionResult> PostDummyWA([FromBody] AssetModel request)
        {
            try
            {
                var result = await _DummyService.PostData(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
