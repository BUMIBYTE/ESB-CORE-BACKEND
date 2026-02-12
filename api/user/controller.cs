

using Beres.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Trasgo.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/v1/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _IUserService;
        private readonly ErrorHandlingUtility _errorUtility;
        private readonly ValidationMasterDto _masterValidationService;
        private readonly IMongoCollection<User> Users;
        private readonly ConvertJWT _ConvertJwt;
        public UserController(IUserService UserService, ConvertJWT convert, IConfiguration configuration)
        {
            _IUserService = UserService;
            _ConvertJwt = convert;
            _errorUtility = new ErrorHandlingUtility();
            _masterValidationService = new ValidationMasterDto();
            MongoClient client = new MongoClient(configuration.GetConnectionString("ConnectionURI"));
            IMongoDatabase database = client.GetDatabase("beres2db");
            Users = database.GetCollection<User>("User");

        }

        // [Authorize]
        [HttpGet]
        public async Task<object> Get()
        {
            try
            {
                if (!IsClaimsValid())
                {
                    throw new CustomException(400, "Error", "Unauthorized");
                }

                string idUser = await GetUserIdFromJwtAsync();

                if (!await IsUserRoleAllowedAsync(idUser))
                {
                    throw new CustomException(400, "Error", "Account not allowed");
                }

                var data = await _IUserService.Get();
                return Ok(data);
            }
            catch (CustomException ex)
            {
                int errorCode = ex.ErrorCode;
                var errorResponse = new ErrorResponse(errorCode, ex.ErrorHeader, ex.Message);
                return _errorUtility.HandleError(errorCode, errorResponse);
            }
        }

        [HttpGet("MemberKoperasi")]
        public async Task<object> GetMember()
        {
            try
            {
                if (!IsClaimsValid())
                {
                    throw new CustomException(400, "Error", "Unauthorized");
                }

                string idUser = await GetUserIdFromJwtAsync();

                if (!await IsUserRoleAllowedAsync(idUser))
                {
                    throw new CustomException(400, "Error", "Account not allowed");
                }

                var data = await _IUserService.GetMember();
                return Ok(data);
            }
            catch (CustomException ex)
            {
                int errorCode = ex.ErrorCode;
                var errorResponse = new ErrorResponse(errorCode, ex.ErrorHeader, ex.Message);
                return _errorUtility.HandleError(errorCode, errorResponse);
            }
        }

        // [HttpGet("{id}")]
        // public async Task<object> GetById([FromRoute] string id)
        // {
        //     try
        //     {
        //         var data = await _IUserService.GetById(id);
        //         return Ok(data);
        //     }
        //     catch (CustomException ex)
        //     {
        //         int errorCode = ex.ErrorCode;
        //         var errorResponse = new ErrorResponse(errorCode, ex.ErrorHeader, ex.Message);
        //         return _errorUtility.HandleError(errorCode, errorResponse);
        //     }
        // }

        // [Authorize]
        [HttpPost("Transfer")]
        public async Task<object> TransferBalance([FromBody] CreateTransferDto item)
        {
            try
            {
                string idUser = await GetUserIdFromJwtAsync();
                var data = await _IUserService.TransferBalance(item, idUser);
                return Ok(data);
            }
            catch (CustomException ex)
            {
                int errorCode = ex.ErrorCode;
                var errorResponse = new ErrorResponse(errorCode, ex.ErrorHeader, ex.Message);
                return _errorUtility.HandleError(errorCode, errorResponse);
            }
        }

        [HttpPost("AddUser")]
        public async Task<object> AddUser([FromBody] CreateUserDto item)
        {
            try
            {
                if (!IsClaimsValid())
                {
                    throw new CustomException(400, "Error", "Unauthorized");
                }

                string idUser = await GetUserIdFromJwtAsync();

                if (!await IsUserRoleAllowedAsync(idUser))
                {
                    throw new CustomException(400, "Error", "Account not allowed");
                }
                var data = await _IUserService.AddUser(item);
                return Ok(data);
            }
            catch (CustomException ex)
            {
                int errorCode = ex.ErrorCode;
                var errorResponse = new ErrorResponse(errorCode, ex.ErrorHeader, ex.Message);
                return _errorUtility.HandleError(errorCode, errorResponse);
            }
        }

        // // [Authorize]
        // [HttpPut("{id}")]
        // public async Task<object> Put([FromRoute] string id, [FromBody] CreateUserDto item)
        // {
        //     try
        //     {
        //         var data = await _IUserService.Put(id, item);
        //         return Ok(data);
        //     }
        //     catch (CustomException ex)
        //     {
        //         int errorCode = ex.ErrorCode;
        //         var errorResponse = new ErrorResponse(errorCode, ex.ErrorHeader, ex.Message);
        //         return _errorUtility.HandleError(errorCode, errorResponse);
        //     }
        // }

        // // [Authorize]
        // [HttpDelete("{id}")]
        // public async Task<object> Delete([FromRoute] string id)
        // {
        //     try
        //     {
        //         var data = await _IUserService.Delete(id);
        //         return Ok(data);
        //     }
        //     catch (CustomException ex)
        //     {
        //         int errorCode = ex.ErrorCode;
        //         var errorResponse = new ErrorResponse(errorCode, ex.ErrorHeader, ex.Message);
        //         return _errorUtility.HandleError(errorCode, errorResponse);
        //     }
        // }

        private bool IsClaimsValid()
        {
            return User?.Claims != null && User.Claims.Any();
        }

        private async Task<string> GetUserIdFromJwtAsync()
        {
            string accessToken = HttpContext.Request.Headers["Authorization"];
            return await _ConvertJwt.ConvertString(accessToken);
        }

        private async Task<bool> IsUserRoleAllowedAsync(string idUser)
        {
            var user = await Users.Find(_ => _.Phone == idUser).FirstOrDefaultAsync();
            return user != null && user.IdRole == "2";
        }


    }
}