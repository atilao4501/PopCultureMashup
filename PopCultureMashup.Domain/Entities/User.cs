using System;

namespace PopCultureMashup.Domain.Entities
{
    /// <summary>
    /// Represents an end user of the system.
    /// Stores basic identity information (name, creation date).
    /// Links to seeds, recommendations, feedback, and weights.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Unique identifier for the user.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The display name of the user.
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// The date and time when the user account was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
