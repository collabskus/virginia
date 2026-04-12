using Virginia.Data;

namespace Virginia.Services;

/// <summary>
/// Generates realistic-looking fake <see cref="Contact"/> entities for seeding/testing.
/// Uses no external dependencies — just arrays of common names, cities, and states.
/// </summary>
internal static class FakeContactGenerator
{
    private static readonly string[] FirstNames =
    [
        "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda",
        "David", "Elizabeth", "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica",
        "Thomas", "Sarah", "Christopher", "Karen", "Charles", "Lisa", "Daniel", "Nancy",
        "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley",
        "Steven", "Kimberly", "Andrew", "Emily", "Paul", "Donna", "Joshua", "Michelle",
        "Kenneth", "Carol", "Kevin", "Amanda", "Brian", "Dorothy", "George", "Melissa",
        "Timothy", "Deborah", "Ronald", "Stephanie", "Edward", "Rebecca", "Jason", "Sharon",
        "Jeffrey", "Laura", "Ryan", "Cynthia", "Jacob", "Kathleen", "Gary", "Amy",
        "Nicholas", "Angela", "Eric", "Shirley", "Jonathan", "Anna", "Stephen", "Brenda",
        "Larry", "Pamela", "Justin", "Emma", "Scott", "Nicole", "Brandon", "Helen",
        "Benjamin", "Samantha", "Samuel", "Katherine", "Raymond", "Christine", "Gregory", "Debra",
        "Frank", "Rachel", "Alexander", "Carolyn", "Patrick", "Janet", "Jack", "Catherine",
        "Dennis", "Maria", "Jerry", "Heather", "Tyler", "Diane", "Aaron", "Ruth",
        "Jose", "Julie", "Adam", "Olivia", "Nathan", "Joyce", "Henry", "Virginia",
        "Peter", "Victoria", "Zachary", "Kelly", "Douglas", "Lauren", "Harold", "Christina"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
        "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
        "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker",
        "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill",
        "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell",
        "Mitchell", "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz",
        "Parker", "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales",
        "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson",
        "Bailey", "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward",
        "Richardson", "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray",
        "Mendoza", "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel",
        "Myers", "Long", "Ross", "Foster", "Jimenez", "Powell", "Jenkins", "Perry",
        "Russell", "Sullivan", "Bell", "Coleman", "Butler", "Henderson", "Barnes", "Gonzales",
        "Fisher", "Vasquez", "Simmons", "Griffin", "Franklin", "Wallace", "Spencer", "Wong"
    ];

    private static readonly string[] Streets =
    [
        "Main St", "Oak Ave", "Maple Dr", "Cedar Ln", "Elm St", "Pine Rd",
        "Washington Blvd", "Park Ave", "Lake Dr", "Hill St", "River Rd", "Forest Ln",
        "Sunset Blvd", "Broadway", "Church St", "Spring St", "Highland Ave", "Meadow Ln",
        "Valley Rd", "Lincoln Ave", "Jefferson St", "Adams St", "Franklin Dr", "Monroe Ct",
        "Madison Ave", "Harrison Blvd", "Grant St", "Cleveland Ave", "McKinley Rd", "Roosevelt Way"
    ];

    private static readonly string[] Cities =
    [
        "Richmond", "Virginia Beach", "Norfolk", "Chesapeake", "Arlington",
        "Newport News", "Alexandria", "Hampton", "Roanoke", "Lynchburg",
        "Ashburn", "Charlottesville", "Fairfax", "Fredericksburg", "Manassas",
        "Winchester", "Harrisonburg", "Williamsburg", "Leesburg", "Blacksburg",
        "New York", "Los Angeles", "Chicago", "Houston", "Phoenix",
        "Philadelphia", "San Antonio", "San Diego", "Dallas", "Austin",
        "Jacksonville", "San Francisco", "Columbus", "Indianapolis", "Charlotte",
        "Seattle", "Denver", "Nashville", "Portland", "Atlanta"
    ];

    private static readonly string[] States =
    [
        "VA", "NY", "CA", "TX", "FL", "IL", "PA", "OH", "GA", "NC",
        "MI", "NJ", "AZ", "WA", "CO", "MA", "TN", "IN", "MO", "MD"
    ];

    private static readonly string[] EmailDomains =
    [
        "example.com", "test.org", "sample.net", "demo.io", "mail.example.com",
        "fakeinbox.org", "placeholder.dev", "notreal.biz", "testmail.co", "nomail.example"
    ];

    private static readonly string[] EmailLabels = ["Personal", "Work", "Other"];
    private static readonly string[] PhoneLabels = ["Mobile", "Home", "Work", "Other"];
    private static readonly string[] AddressLabels = ["Home", "Work", "Other"];

    public static Contact Generate(Random rng)
    {
        var firstName = Pick(rng, FirstNames);
        var lastName = Pick(rng, LastNames);
        var now = DateTime.UtcNow;

        var contact = new Contact
        {
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Emails = [],
            Phones = [],
            Addresses = []
        };

        // 80% chance of having an email, 30% chance of a second
        if (rng.Next(100) < 80)
        {
            contact.Emails.Add(new ContactEmail
            {
                Label = "Personal",
                Address = GenerateEmail(rng, firstName, lastName)
            });

            if (rng.Next(100) < 30)
            {
                contact.Emails.Add(new ContactEmail
                {
                    Label = "Work",
                    Address = GenerateEmail(rng, firstName, lastName)
                });
            }
        }

        // 70% chance of having a phone
        if (rng.Next(100) < 70)
        {
            contact.Phones.Add(new ContactPhone
            {
                Label = Pick(rng, PhoneLabels),
                Number = GeneratePhone(rng)
            });
        }

        // 60% chance of having an address
        if (rng.Next(100) < 60)
        {
            contact.Addresses.Add(new ContactAddress
            {
                Label = Pick(rng, AddressLabels),
                Street = $"{rng.Next(100, 9999)} {Pick(rng, Streets)}",
                City = Pick(rng, Cities),
                State = Pick(rng, States),
                PostalCode = rng.Next(10000, 99999).ToString("D5"),
                Country = "US"
            });
        }

        return contact;
    }

    private static string Pick(Random rng, string[] array) =>
        array[rng.Next(array.Length)];

    private static string GenerateEmail(Random rng, string first, string last)
    {
        var domain = Pick(rng, EmailDomains);
        var separator = rng.Next(3) switch
        {
            0 => ".",
            1 => "_",
            _ => ""
        };
        var suffix = rng.Next(1, 999);
        return $"{first.ToLowerInvariant()}{separator}{last.ToLowerInvariant()}{suffix}@{domain}";
    }

    private static string GeneratePhone(Random rng)
    {
        var area = rng.Next(200, 999);
        var prefix = rng.Next(200, 999);
        var line = rng.Next(1000, 9999);
        return $"({area}) {prefix}-{line}";
    }
}
