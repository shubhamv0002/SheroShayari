using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SheroShayari.API.Models;

namespace SheroShayari.API.Data;

/// <summary>
/// Entity Framework Core database context for SheroShayari application with Identity support.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Initializes a new instance of the AppDbContext.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Shayaris database set.
    /// </summary>
    public DbSet<Shayari> Shayaris { get; set; }

    /// <summary>
    /// Configures the model upon creation.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Shayari entity
        modelBuilder.Entity<Shayari>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.Poet).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Meaning).HasMaxLength(5000);
            entity.Property(e => e.IsAiGenerated).HasDefaultValue(false);
            entity.Property(e => e.IsPublic).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.Shayaris)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ApplicationUser entity
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(256);
            entity.Property(e => e.PreferredLanguage).HasDefaultValue("Hindi");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
        });

        // Seed initial data with classic Shayaris (no user association)
        SeedInitialData(modelBuilder);
    }

    /// <summary>
    /// Seeds the database with initial Shayari data.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shayari>().HasData(
            new Shayari
            {
                Id = 1,
                Content = "ये फलक न मेरे लिए कहीं ऊँचा नहीं न ज़मीन न दिगंत\nइस शरारे काइनात में मेरा कोई पहचान नहीं",
                Poet = "Mirza Ghalib",
                Language = "Hindi",
                Category = "Philosophy",
                Meaning = "This sky is not high, neither the earth nor horizon - in this universe, I have no identity.",
                IsAiGenerated = false,
                IsPublic = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },
            new Shayari
            {
                Id = 2,
                Content = "हज़ारों ख़्वाहिशें ऐसी के हर ख़्वाहिश पे दम निकले\nबहुत निकले मेरे दिन ग़मों में यह भी कम निकले",
                Poet = "Mirza Ghalib",
                Language = "Hindi",
                Category = "Love",
                Meaning = "Thousands of desires, each worth dying for - yet my days have passed in sorrow, and this too seems little.",
                IsAiGenerated = false,
                IsPublic = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },
            new Shayari
            {
                Id = 3,
                Content = "हम देखेंगे लाज़िम है कि हम भी देखेंगे\nवो दिन कि जिसका वादा है",
                Poet = "Faiz Ahmed Faiz",
                Language = "Hindi",
                Category = "Hope",
                Meaning = "We shall witness it, inevitably we shall see that day for which the promise has been made.",
                IsAiGenerated = false,
                IsPublic = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },
            new Shayari
            {
                Id = 4,
                Content = "प्राणों से भी अधिक मेरे यह रूप गवाँ दूँगा\nपर यह दे सकूँ अगर तो फिर क्या कहूँ मैं",
                Poet = "Kumarpala",
                Language = "Hindi",
                Category = "Sacrifice",
                Meaning = "I can lose my form and life itself, but if I could give that to you, what more could I say?",
                IsAiGenerated = false,
                IsPublic = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            },
            new Shayari
            {
                Id = 5,
                Content = "चाँदनी रात में फूल का सपना देखता हूँ\nतेरी यादों में खो जाता हूँ मैं सदा",
                Poet = "Unknown",
                Language = "Hindi",
                Category = "Love",
                Meaning = "In moonlit nights I dream of flowers, and in your memories, I am always lost.",
                IsAiGenerated = false,
                IsPublic = true,
                UserId = null,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
