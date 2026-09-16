using ChatApp.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.CodeDom.Compiler;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace ChatApp.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ChatAppDbContext chatAppDbContext;
        private readonly IConfiguration configuration;
        public AuthController(ChatAppDbContext chatAppDbContext, IConfiguration configuration)
        {
            this.chatAppDbContext = chatAppDbContext;
            this.configuration = configuration;
        }
        //class nhận dữ liệu từ clients
        public class LoginRequest
        {
            public string UserName { get; set; }
            public string Password { get; set; }
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] LoginRequest request)
        {
            var userExists = await chatAppDbContext.Users.AnyAsync(u => u.Username == request.UserName);
            if (userExists)
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });
            }
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newUser = new User
            {
                Username=request.UserName,
                PasswordHash=hashedPassword,
                IsOnline=false,
                CreatedAt=DateTime.Now
            };
            chatAppDbContext.Users.Add(newUser);
            await chatAppDbContext.SaveChangesAsync();
            return Ok(new { message = "Đăng ký thành công!" });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            //tìm user trong db bằng username
            var user = await chatAppDbContext.Users.FirstOrDefaultAsync(u => u.Username == request.UserName);
            //kiểm tra tài khoản và mật khẩu
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu!" });
            }
            //nếu đúng, tạo token
            var token = GenerateJwtToken(user);
            return Ok(new
            {
                Token = token,
                UserId = user.UserId,
                UserName = user.Username
            });
        }
        private string GenerateJwtToken(User user)
        {
            //lấy key từ appsetting
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            //định nghĩa các thông tin claims gói gọn bên trong token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            //cấu hình thời hạn của token
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
