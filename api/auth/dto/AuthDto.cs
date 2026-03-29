using System.Text.Json.Serialization;

public class LoginDto
{
    public string? Email { get; set; }
    public string? Pin { get; set; }
}

public class LoginGoogleDto
{
    public string? Token { get; set; }
}

public class RegisterDto
{
    public string? Pin { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }

}

public class RegisterGoogleDto
{
    public string? Token { get; set; }
    public string? Username { get; set; }

}

public class UpdatePasswordDto
{
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }

}

public class ForgotPasswordDto
{
    public string? CodeOtp { get; set; }
    public string? Password { get; set; }

}

public class UpdatePinDto
{
    public string? Pin { get; set; }
    public string? ConfirmPin { get; set; }

}

public class UpdateProfileDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? NoNIK { get; set; }
}

public class UpdateFCMProfileDto
{
    public string? FCM { get; set; }
}


public class DriverStatusServe
{
    public bool IsStandby { get; set; }  
}

public class DriverServiceServe
{
    public List<string> Service { get; set; }  
}

public class PayloadNotifSend
{
    public string FCM { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string? IdOrder { get; set; }
    public string? Image { get; set; }



}

public class PinDto
{
    public string? Pin { get; set; }

}

public class OtpDto
{
    public string? Email { get; set; }
    public string? Otp { get; set; }
}

public class ReqOtpDto
{
    public string? Email { get; set; }
}

public class ChangeUserPasswordDto {
    public string? currentPassword { get; set; }

    public string? newPassword { get; set; }

}

public class ZoomGetDto {
    public string MeetingNumber { get; set; }
        public int Role { get; set; } // 0 for participant, 1 for host
        public int? ExpirationSeconds { get; set; }

}

public class JwtPayloads
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("picture")]
    public string Picture { get; set; }

    [JsonPropertyName("iss")]
    public string Issuer { get; set; }

    [JsonPropertyName("aud")]
    public string Audience { get; set; }

    [JsonPropertyName("auth_time")]
    public long AuthTime { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("sub")]
    public string Subject { get; set; }

    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    [JsonPropertyName("exp")]
    public long Expiration { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("firebase")]
    public FirebaseInfo Firebase { get; set; }
}

public class FirebaseInfo
{
    [JsonPropertyName("identities")]
    public FirebaseIdentities Identities { get; set; }

    [JsonPropertyName("sign_in_provider")]
    public string SignInProvider { get; set; }
}

public class FirebaseIdentities
{
    [JsonPropertyName("google.com")]
    public List<string> Google { get; set; }

    [JsonPropertyName("email")]
    public List<string> Email { get; set; }
}
