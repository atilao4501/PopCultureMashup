using Microsoft.AspNetCore.Identity;

namespace PopCultureMashup.Infrastructure.Auth.Entities
{
    /// <summary>
    /// Represents an end user of the system.
    /// Stores basic identity information (name, creation date).
    /// Links to seeds, recommendations, feedback, and weights.
    /// </summary>
    public class User : IdentityUser<Guid>
    {
        /// <summary>
        /// The date and time when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
