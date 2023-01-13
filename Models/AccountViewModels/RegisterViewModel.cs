using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Traffic.Models.AccountViewModels
{
    public class RegisterViewModel
    {
    public string Id { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    [Required]
    [Display(Name = "Max Journey Count to Create")]
    [Range(1, 100)]
    public int MaxJourneyCount { get; set; }

    [Required]
    [Display(Name = "Max Count to run Journey per day")]
    [Range(1, 10)]
    public int MaxRunCountPerDay { get; set; }

    [Required]
    [Display(Name = "Create jobs from file")]
    public bool EnableCreateJobFromFile { get; set; }

    [Required]
    [Display(Name = "Use Google Search Console to create journey")]
    public bool EnableGoogleSearchConsole { get; set; }

    [Required]
    [Display(Name = "Enable User to see result log")]
    public bool EnableSeeAdminLog { get; set; }

    [Required]
    [Display(Name = "Enable User to interupt schedule")]
    public bool EnableScheduleInterupt { get; set; }

    [Required]
    [Display(Name = "Enable User to set Geo Location")]
    public bool EnableGeoLocation { get; set; }

    [Required]
    [Display(Name = "Enable User to use GMB Feature")]
    public bool EnableGMB { get; set; }

    [Required]
    [Display(Name = "Enable User to use Html Builder")]
    public bool EnableHtml { get; set; }

    [Required]
    [Display(Name = "Enable User to use Twitter")]
    public bool EnableTwitter { get; set; }

    [Display(Name = "Select journey options available to User")]
    public List<CheckboxJourney> JourneyOptions { get; set; }
    }
}
