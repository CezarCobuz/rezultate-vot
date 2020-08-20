using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Models;
using ElectionResults.Core.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectionResults.WebApi.Controllers
{
    [Route("api/admin")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IElectionConfigurationSource _electionConfigurationSource;
        private readonly IAdminRepository _adminRepository;

        public AdminController(IElectionConfigurationSource electionConfigurationSource, IAdminRepository adminRepository)
        {
            _electionConfigurationSource = electionConfigurationSource;
            _adminRepository = adminRepository;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var authenticationResult = _adminRepository.GetByUsernameAndPassword(model.Email, model.Password);
            if (authenticationResult.IsFailure)
                return Unauthorized();

            var user = authenticationResult.Value;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Administrator")
            };

            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return Ok(new
            {
                user.UserName
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }

        [HttpPut("interval")]
        public async Task<ActionResult> SetInterval([FromBody] int interval)
        {
            var result = await _electionConfigurationSource.UpdateInterval(interval);
            if (result.IsSuccess)
                return Ok();
            return BadRequest(result.Error);
        }

        [HttpGet("elections-config")]
        [AllowAnonymous]
        public async Task<ActionResult> GetSettings()
        {
            var result = await _electionConfigurationSource.GetConfigAsync();
            if (result.IsSuccess)
                return Ok(result.Value);
            return BadRequest(result.Error);
        }

        [HttpPut("elections-config")]
        public async Task<ActionResult> UpdateSettings([FromBody] List<Election> elections)
        {
            await _electionConfigurationSource.UpdateElectionConfig(elections);
            return Ok();
        }
    }
}