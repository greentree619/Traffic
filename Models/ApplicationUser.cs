using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Traffic.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
    public int MaxJourneyCount { get; set; }

    public int MaxRunCountPerDay { get; set; }

    public string JourneyOptionList { get; set; }

    public bool EnableCreateJobFromFile { get; set; }

    public bool EnableGoogleSearchConsole { get; set; }

    public bool EnableSeeAdminLog { get; set; }

    public bool EnableScheduleInterupt { get; set; }

    public bool EnableGeoLocation { get; set; }

    public bool EnableGMB { get; set; }

    public bool EnableHtml { get; set; }

    public bool EnableTwitter { get; set; }
    }
}
