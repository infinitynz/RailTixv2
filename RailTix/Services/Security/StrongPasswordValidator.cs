using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using RailTix.Models.Domain;

namespace RailTix.Services.Security
{
    public class StrongPasswordValidator : IPasswordValidator<ApplicationUser>
    {
        // Small denylist; for production, consider using HaveIBeenPwned APIs.
        private static readonly HashSet<string> Common = new(new[]
        {
            "password","123456","qwerty","letmein","admin","welcome","railtix","iloveyou","monkey","dragon"
        });

        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
        {
            var errors = new List<IdentityError>();
            if (string.IsNullOrEmpty(password))
            {
                errors.Add(new IdentityError { Description = "Password is required." });
                return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
            }

            var lower = password.ToLowerInvariant();
            if (Common.Contains(lower))
                errors.Add(new IdentityError { Description = "Password is too common." });

            // Personal info check removed per policy decision

            // Enforce composition (server-side double check)
            if (!Regex.IsMatch(password, @"[a-z]")) errors.Add(new IdentityError { Description = "Password must include a lowercase letter." });
            if (!Regex.IsMatch(password, @"[A-Z]")) errors.Add(new IdentityError { Description = "Password must include an uppercase letter." });
            if (!Regex.IsMatch(password, @"\d")) errors.Add(new IdentityError { Description = "Password must include a digit." });
            if (!Regex.IsMatch(password, @"[^a-zA-Z0-9\s]")) errors.Add(new IdentityError { Description = "Password must include a symbol." });

            return Task.FromResult(errors.Any() ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success);
        }
    }
}

