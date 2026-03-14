using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepositoryPattern.Services.RedPayService;
using System.Text.Json;
using System.Threading.Tasks;

namespace Beres.Server.Controllers
{
    // [Authorize]
    [ApiController]
    [Route("api/v1/primakom")]
    public class RedPayController : ControllerBase
    {
        private readonly IRedPayService _RedPayService;
        private readonly ConvertJWT _ConvertJwt;

        public RedPayController(ConvertJWT convert,IRedPayService RedPayService)
        {
            _RedPayService = RedPayService;
            _ConvertJwt = convert;
        }

        // [HttpPost("generate-bodysign")]
        // public async Task<IActionResult> GenerateBodysign([FromBody] CreateRedpayDto dto)
        // {
        //     try
        //     {
        //         var payload = new
        //         {
        //             redirect_url = "https://merchant.com/return",
        //             user_id = "20250209TEST3477000000",
        //             user_mdn = "08123412451",
        //             merchant_transaction_id = "TESTSH0000011",
        //             payment_method = "indosat_airtime",
        //             currency = "IDR",
        //             amount = 5000,
        //             item_id = "1",
        //             item_name = "PAYMENT",
        //             notification_url = "https://merchant/callback-payment"
        //         };

        //         string appSecret = "ee9Kppp-tBUmRRFM";
        //         string bodysign = RedPayService.GenerateBodySign(payload, appSecret);
        //         return Ok(new { message = bodysign });
        //     }
        //     catch (Exception ex)
        //     {
        //         return BadRequest(new { message = ex.Message });
        //     }
        // }

        [HttpGet("sharepoint")]
        public async Task<IActionResult> GetRedPayWA()
        {
            try
            {
                var result = await _RedPayService.GetData();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // [HttpGet("esb")]
        // public async Task<IActionResult> GetDataESB()
        // {
        //     try
        //     {                var result = await _RedPayService.GetDataESB();
        //         return Ok(result);      
        //     }
        //     catch (Exception ex)
        //     {
        //         return BadRequest(new { message = ex.Message });
        //     }
        // }

        [HttpPost("sap")]
        public async Task<IActionResult> PostRedPayWA([FromBody] JsonElement request)
        {
            try
            {                var result = await _RedPayService.PostData(request);
                return Ok(result);      
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
