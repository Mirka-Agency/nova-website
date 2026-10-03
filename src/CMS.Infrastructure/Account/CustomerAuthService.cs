using CMS.Application.Account;
using CMS.Application.Auth;
using CMS.Application.Sms;
using CMS.Domain.Exceptions;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CMS.Infrastructure.Account;

public sealed class CustomerAuthService : ICustomerAuthService
{
    public const int OtpTtlMinutes = 5;
    public const int OtpResendSeconds = 120;
    public const int MaxAttempts = 8;

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISmsSender _sms;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CustomerAuthService> _logger;

    public CustomerAuthService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor,
        ISmsSender sms,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<CustomerAuthService> logger)
    {
        _db = db;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _sms = sms;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<RequestOtpResult> RequestOtpAsync(string phone, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeIranMobile(phone)
            ?? throw new DomainException("شماره موبایل نامعتبر است.");

        var now = DateTime.UtcNow;
        var challenge = await _db.OtpChallenges
            .FirstOrDefaultAsync(c => c.Phone == normalized, cancellationToken);

        if (challenge is not null)
        {
            var elapsed = now - challenge.LastSentAtUtc;
            if (elapsed < TimeSpan.FromSeconds(OtpResendSeconds))
            {
                var retryAfter = (int)Math.Ceiling((TimeSpan.FromSeconds(OtpResendSeconds) - elapsed).TotalSeconds);
                return new RequestOtpResult(false, $"لطفاً {retryAfter} ثانیه دیگر دوباره تلاش کنید.", retryAfter);
            }
        }

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var hash = HashCode(normalized, code);

        if (_environment.IsDevelopment())
            _logger.LogInformation("OTP code for {Phone}: {Code}", normalized, code);

        if (challenge is null)
        {
            challenge = new OtpChallenge
            {
                Phone = normalized,
                CodeHash = hash,
                ExpiresAtUtc = now.AddMinutes(OtpTtlMinutes),
                LastSentAtUtc = now,
                AttemptCount = 0
            };
            _db.OtpChallenges.Add(challenge);
        }
        else
        {
            challenge.CodeHash = hash;
            challenge.ExpiresAtUtc = now.AddMinutes(OtpTtlMinutes);
            challenge.LastSentAtUtc = now;
            challenge.AttemptCount = 0;
            challenge.MarkUpdated();
        }

        await _db.SaveChangesAsync(cancellationToken);

        var smsResult = await _sms.SendAsync(
            new SmsMessage(normalized, $"کد ورود شما: {code}\nاین کد تا {OtpTtlMinutes} دقیقه معتبر است."),
            cancellationToken);

        if (!smsResult.Succeeded && !smsResult.Skipped)
        {
            _logger.LogWarning("OTP SMS failed for {Phone}: {Reason}", normalized, smsResult.ProviderMessage);
            return new RequestOtpResult(false, "ارسال پیامک ناموفق بود. لطفاً دوباره تلاش کنید.");
        }

        if (smsResult.Skipped)
            _logger.LogInformation("OTP SMS skipped (provider disabled) for {Phone}", normalized);

        return new RequestOtpResult(true, null, OtpResendSeconds);
    }

    public async Task<VerifyOtpResult> VerifyOtpAsync(string phone, string code, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeIranMobile(phone)
            ?? throw new DomainException("شماره موبایل نامعتبر است.");

        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 6)
            return new VerifyOtpResult(false, "کد تأیید نامعتبر است.");

        var challenge = await _db.OtpChallenges
            .FirstOrDefaultAsync(c => c.Phone == normalized, cancellationToken);

        if (challenge is null)
            return new VerifyOtpResult(false, "ابتدا درخواست کد تأیید دهید.");

        if (challenge.ExpiresAtUtc < DateTime.UtcNow)
            return new VerifyOtpResult(false, "کد منقضی شده است. دوباره درخواست دهید.");

        if (challenge.AttemptCount >= MaxAttempts)
            return new VerifyOtpResult(false, "تعداد تلاش بیش از حد مجاز است. دوباره درخواست کد دهید.");

        challenge.AttemptCount++;
        challenge.MarkUpdated();

        var expected = HashCode(normalized, code.Trim());
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(challenge.CodeHash)))
        {
            await _db.SaveChangesAsync(cancellationToken);
            return new VerifyOtpResult(false, "کد تأیید نادرست است.");
        }

        _db.OtpChallenges.Remove(challenge);
        await _db.SaveChangesAsync(cancellationToken);

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalized, cancellationToken);
        var isNew = false;
        if (user is null)
        {
            isNew = true;
            user = new ApplicationUser
            {
                UserName = normalized,
                PhoneNumber = normalized,
                PhoneNumberConfirmed = true,
                Email = $"{normalized}@customers.local",
                EmailConfirmed = false
            };

            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                var error = string.Join(" ", create.Errors.Select(e => e.Description));
                return new VerifyOtpResult(false, string.IsNullOrWhiteSpace(error) ? "ایجاد حساب ناموفق بود." : error);
            }
        }
        else if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        await SignInCustomerAsync(user);
        return new VerifyOtpResult(true, null, user.Id, isNew);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required.");
        await context.SignOutAsync(CustomerAuthDefaults.AuthenticationScheme);
    }

    private async Task SignInCustomerAsync(ApplicationUser user)
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName ?? user.PhoneNumber ?? user.UserName ?? user.Id),
            new(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty)
        };
        if (!string.IsNullOrWhiteSpace(user.Email) && !user.Email.EndsWith("@customers.local", StringComparison.OrdinalIgnoreCase))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        var identity = new ClaimsIdentity(claims, CustomerAuthDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await context.SignInAsync(
            CustomerAuthDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });
    }

    public static string? NormalizeIranMobile(string? phone)
    {
        var normalized = SmsPhoneNormalizer.Normalize(phone);
        if (normalized is null)
            return null;

        if (normalized.Length != 11 || !normalized.StartsWith("09", StringComparison.Ordinal))
            return null;

        return normalized;
    }

    private string HashCode(string phone, string code)
    {
        var pepper = _configuration["Security:OtpPepper"];
        var material = string.IsNullOrWhiteSpace(pepper)
            ? $"{phone}:{code}"
            : $"{pepper}:{phone}:{code}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(bytes);
    }
}
