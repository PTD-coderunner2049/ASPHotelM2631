using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ASPHotelM2631.Application.Authentication;
using ASPHotelM2631.Domain.Constants;
using ASPHotelM2631.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ASPHotelM2631.Tests.Api;

/// <summary>
/// Chạy TOÀN BỘ pipeline thật của ứng dụng (Program.cs: Identity, JWT, middleware, controller)
/// in-process. Chỉ thay SQL Server bằng InMemory; mỗi factory có database riêng nên các test không dính nhau.
/// Vì chạy qua pipeline thật nên bắt được lỗi mà unit test không thấy (cấu hình JWT, claim, middleware...).
/// </summary>
public sealed class HotelApiFactory : WebApplicationFactory<ASPHotelM2631.Web.Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    /// <summary>true = seed 6 role như môi trường thật cần có. false = DB trống (đúng như khi mới migrate).</summary>
    public bool SeedRoles { get; init; } = true;
    // Keep integration tests out of Program's Development-only SQL migration path;
    // this factory replaces SQL Server with EF InMemory.
    public string EnvName { get; init; } = "Testing";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvName);
        builder.UseSetting("https_port", "7069");          // để UseHttpsRedirection biết cổng HTTPS

        builder.ConfigureServices(services =>
        {
            // EF Core 9 đăng ký thêm IDbContextOptionsConfiguration<AppDbContext>; phải gỡ MỌI đăng ký liên quan
            // đến AppDbContext, nếu không sẽ lỗi "hai database provider".
            var old = services.Where(d => d.ServiceType == typeof(AppDbContext) ||
                                          (d.ServiceType.IsGenericType &&
                                           d.ServiceType.GenericTypeArguments.Contains(typeof(AppDbContext)))).ToList();
            foreach (var d in old) services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, CapturingEmailSender>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        if (SeedRoles) SeedRolesAsync(host.Services).GetAwaiter().GetResult();
        return host;
    }

    private static async Task SeedRolesAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in RoleNames.All)
            if (!await roles.RoleExistsAsync(name))
                (await roles.CreateAsync(new IdentityRole(name))).ThrowIfFailed();
    }
}

internal sealed class CapturingEmailSender : IEmailSender
{
    private static readonly ConcurrentDictionary<string, string> Codes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex CodePattern = new("<h2[^>]*>(?<code>\\d{6})</h2>", RegexOptions.Compiled);

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        var match = CodePattern.Match(htmlBody);
        if (match.Success) Codes[to] = match.Groups["code"].Value;
        return Task.CompletedTask;
    }

    public static bool TryTakeCode(string email, out string code) => Codes.TryRemove(email, out code!);
}

internal static class IdentityResultExtensions
{
    public static void ThrowIfFailed(this IdentityResult r)
    {
        if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Code)));
    }
}
