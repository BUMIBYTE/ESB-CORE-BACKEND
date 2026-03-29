
public interface IAuthService
{

    Task<object> LoginAsync(LoginDto login);
    Task<object> Aktifasi(string id);

}