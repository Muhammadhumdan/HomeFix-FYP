using HomeFix.API.Interfaces;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Data;

public static class SeedData
{
    /// <summary>Creates the demo data once. Skipped automatically when the database already contains users.</summary>
    public static async Task InitializeAsync(HomeFixDbContext db, IPasswordService hasher)
    {
        if (await db.Users.AnyAsync()) return;

        var cats = new[]
        {
            new ServiceCategory { Name = "Electrician", Icon = "⚡", Description = "Wiring, fans, switches, UPS and electrical repairs" },
            new ServiceCategory { Name = "Plumber", Icon = "🔧", Description = "Leaks, pipes, taps, geysers and bathroom fittings" },
            new ServiceCategory { Name = "Painter", Icon = "🎨", Description = "Interior and exterior painting and polishing" },
            new ServiceCategory { Name = "Carpenter", Icon = "🪚", Description = "Furniture, doors, cabinets and woodwork" },
            new ServiceCategory { Name = "Cleaner", Icon = "🧹", Description = "Home, sofa, carpet and deep cleaning" },
            new ServiceCategory { Name = "Mechanic", Icon = "🚗", Description = "Car and bike repair and maintenance" }
        };
        db.ServiceCategories.AddRange(cats);
        var C = cats.ToDictionary(c => c.Name);

        db.Users.Add(new User { FullName = "HomeFix Admin", Email = "admin@homefix.com", PasswordHash = hasher.Hash("Admin@123"), Role = UserRole.Admin, City = "Kohat", Phone = "0300-0000000" });
        var ahmed = new User { FullName = "Ahmed Raza", Email = "customer@homefix.com", PasswordHash = hasher.Hash("Customer@123"), Role = UserRole.Customer, City = "Kohat", Phone = "0311-1234567", Address = "House 12, Muhalla Faqeerabad, Kohat" };
        var sara = new User { FullName = "Sara Khan", Email = "sara@homefix.com", PasswordHash = hasher.Hash("Customer@123"), Role = UserRole.Customer, City = "Kohat", Phone = "0322-7654321", Address = "Street 4, Jarma Road, Kohat" };
        db.Users.AddRange(ahmed, sara);

        ProviderProfile P(string name, string email, string biz, string cat, string area, decimal rate, int exp, string bio,
            ApprovalStatus status, string phone, string days = "Mon,Tue,Wed,Thu,Fri,Sat", params (string title, string desc, decimal price, string unit)[] services)
        {
            var user = new User { FullName = name, Email = email, PasswordHash = hasher.Hash("Provider@123"), Role = UserRole.Provider, City = area.Split(',')[0], Phone = phone };
            var profile = new ProviderProfile
            {
                User = user, BusinessName = biz, Category = C[cat], ServiceArea = area, HourlyRate = rate, ExperienceYears = exp, Bio = bio,
                ApprovalStatus = status, AvailableDays = days, StartTime = "09:00", EndTime = "18:00"
            };
            foreach (var s in services)
                profile.Services.Add(new Service { Category = C[cat], Title = s.title, Description = s.desc, Price = s.price, PriceUnit = s.unit });
            db.ProviderProfiles.Add(profile);
            return profile;
        }

        var ali = P("Ali Khan", "electrician@homefix.com", "Ali Electric Services", "Electrician", "Kohat", 1500, 8,
            "Licensed electrician serving Kohat for 8 years. House wiring, fan and switch repairs, UPS and load-shedding solutions.",
            ApprovalStatus.Approved, "0333-1112223", "Mon,Tue,Wed,Thu,Fri,Sat,Sun",
            ("Fan & Switch Repair", "Repair or replacement of ceiling fans, switches and sockets.", 1500, "hour"),
            ("House Wiring Inspection", "Complete safety inspection of home wiring with a written report.", 2500, "job"),
            ("UPS / Inverter Installation", "Installation and testing of UPS or solar inverter.", 3500, "job"));
        var usman = P("Usman Ghani", "plumber@homefix.com", "Usman Plumbing Services", "Plumber", "Kohat", 1200, 6,
            "Fast and tidy plumbing for homes and shops in Kohat. Leakage repair, tank cleaning and bathroom fittings.",
            ApprovalStatus.Approved, "0345-2223334", "Mon,Tue,Wed,Thu,Fri,Sat",
            ("Leak Repair", "Find and fix water leakage in pipes, taps and walls.", 1200, "hour"),
            ("Bathroom Fitting", "Install commode, basin, shower and taps.", 4000, "job"),
            ("Geyser Installation", "Gas or electric geyser fitting and testing.", 2500, "job"));
        var bilal = P("Bilal Ahmed", "painter@homefix.com", "Bilal Painters & Decorators", "Painter", "Kohat, Peshawar", 1000, 10,
            "Interior and exterior painting with quality materials. Free colour advice.",
            ApprovalStatus.Approved, "0300-3334445", "Mon,Tue,Wed,Thu,Fri,Sat",
            ("Room Painting", "Complete painting of one room including wall preparation.", 8000, "job"),
            ("Exterior Wall Painting", "Weather-proof exterior painting.", 1000, "hour"));
        var zahid = P("Zahid Khan", "electrician2@homefix.com", "Zahid Power Solutions", "Electrician", "Peshawar, Hangu", 1800, 12,
            "Industrial and domestic electrical contractor covering Peshawar and Hangu.",
            ApprovalStatus.Approved, "0312-4445556", "Mon,Tue,Wed,Thu,Fri",
            ("Commercial Wiring", "Wiring for shops and offices.", 1800, "hour"));
        var imran = P("Imran Shah", "carpenter@homefix.com", "Imran Woodworks", "Carpenter", "Kohat", 1400, 9,
            "Custom furniture, doors, wardrobes and repairs.",
            ApprovalStatus.Approved, "0321-5556667", "Mon,Tue,Wed,Thu,Fri,Sat",
            ("Door & Window Repair", "Repair, polish or replacement of wooden doors and windows.", 1400, "hour"),
            ("Custom Wardrobe", "Design and build a wardrobe to your measurements.", 25000, "job"));
        var sana = P("Sana Bibi", "cleaner@homefix.com", "SparkClean Home Services", "Cleaner", "Kohat, Rawalpindi", 900, 5,
            "Professional home cleaning team. Eco-friendly products.",
            ApprovalStatus.Approved, "0334-6667778", "Mon,Tue,Wed,Thu,Fri,Sat",
            ("Deep House Cleaning", "Full home deep clean including kitchen and bathrooms.", 6000, "job"),
            ("Sofa & Carpet Cleaning", "Shampoo cleaning for sofas and carpets.", 900, "hour"));
        var farhan = P("Farhan Ali", "mechanic@homefix.com", "Farhan Auto Care", "Mechanic", "Kohat", 1300, 7,
            "Car and bike repair at your doorstep. Oil change, brakes and diagnostics.",
            ApprovalStatus.Approved, "0301-7778889", "Mon,Tue,Wed,Thu,Fri,Sat",
            ("Car Engine Diagnostics", "Computer scan and inspection.", 1300, "hour"),
            ("Oil Change at Home", "Engine oil and filter change at your doorstep.", 2000, "job"));
        P("Tariq Mehmood", "tariq.electrician@homefix.com", "Tariq Wiring Works", "Electrician", "Kohat", 1100, 4,
            "Affordable electrician for small household jobs in Kohat.", ApprovalStatus.Pending, "0343-8889990", "Mon,Tue,Wed,Thu,Fri,Sat",
            ("Basic Electrical Repair", "Switches, sockets and light fittings.", 1100, "hour"));
        P("Hamid Gul", "hamid.plumber@homefix.com", "Hamid Pipe Fitters", "Plumber", "Kohat", 1000, 3,
            "Plumber for pipe fitting and drain cleaning.", ApprovalStatus.Pending, "0315-9990001", "Mon,Tue,Wed,Thu,Fri,Sat",
            ("Drain Cleaning", "Unblock kitchen and bathroom drains.", 1000, "hour"));

        Booking B(User cust, ProviderProfile p, int svc, int dayOffset, BookingStatus st, string address, string? notes, int rating = 0, string comment = "")
        {
            var service = p.Services[svc];
            var b = new Booking
            {
                Customer = cust, ProviderProfile = p, Service = service, ScheduledDate = DateTime.Today.AddDays(dayOffset),
                Address = address, Notes = notes, EstimatedPrice = service.Price, Status = st,
                CreatedAt = DateTime.UtcNow.AddDays(dayOffset - 2), UpdatedAt = DateTime.UtcNow.AddDays(Math.Min(dayOffset, 0))
            };
            if (rating > 0)
                b.Review = new Review { Customer = cust, ProviderProfile = p, Rating = rating, Comment = comment, CreatedAt = DateTime.UtcNow.AddDays(Math.Min(dayOffset, 0)) };
            db.Bookings.Add(b);
            return b;
        }

        B(ahmed, ali, 0, -10, BookingStatus.Completed, ahmed.Address!, "Bedroom fan was making noise.", 5, "Ali arrived on time and fixed both fans quickly. Very professional and fair price.");
        B(sara, ali, 1, -6, BookingStatus.Completed, sara.Address!, null, 4, "Thorough inspection and clear report. Slightly late but good work.");
        B(ahmed, usman, 0, -4, BookingStatus.Completed, ahmed.Address!, "Kitchen tap leaking.", 5, "Fixed the leak in under an hour. Clean work and polite.");
        B(sara, bilal, 0, -15, BookingStatus.Completed, sara.Address!, "Living room, light blue.", 4, "Great finish and good colour advice. Took one extra day.");
        B(ahmed, sana, 0, -3, BookingStatus.Completed, ahmed.Address!, "Whole house before guests arrive.");   // completed, NOT yet reviewed
        B(ahmed, ali, 0, 2, BookingStatus.Pending, ahmed.Address!, "Tube light flickering in the lounge.");
        B(sara, usman, 1, 1, BookingStatus.Accepted, sara.Address!, "New basin and shower mixer.");
        B(sara, imran, 0, -2, BookingStatus.Rejected, sara.Address!, null);
        B(ahmed, farhan, 1, 3, BookingStatus.Cancelled, ahmed.Address!, null);

        db.Reports.Add(new Report { Reporter = sara, ReportedUser = imran.User, Reason = "Provider did not respond", Details = "Booking was rejected without any message." });

        await db.SaveChangesAsync();
    }
}
