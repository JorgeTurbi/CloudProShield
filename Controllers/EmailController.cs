using Application.DTOs.Auth;
using Commons;
using DTOs.UsersDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Roles;
using Services.UserServices;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IUserCommandCreate _user;
    private readonly IUserCommandRead _userRead;
    private readonly IUserForgotPassword _userForgot;
    public EmailController(IUserCommandCreate user, IUserCommandRead userRead, IUserForgotPassword userForgot)
    {
        _user = user;
        _userRead = userRead;
        _userForgot = userForgot;
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        var response = await _user.ConfirmEmailAsync(token);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("GetByEmail")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByEmail(string email)
    {
        ApiResponse<UserDTO_Only> result = await _userRead.GetUserByEmail(email);
        if (result.Success == false) return BadRequest(new { result });

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasword([FromBody] ForgotPasswordDTO dto)
    {
        var response = await _userForgot.ForgotPasswordAsync(dto.Email, Request.Headers.Origin);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDTO dto)
    {
        var response = await _userForgot.SendOtpAsync(dto.Email, dto.Token);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO dto)
    {
        var response = await _userForgot.VerifyOtpAsync(dto.Email, dto.Token, dto.Otp);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
    {
        var response = await _userForgot.ResetPasswordAsync(dto.Email, dto.Token, dto.Otp, dto.NewPassword, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}