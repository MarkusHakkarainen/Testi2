using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Testi2.Models
{
    public class EditProfileViewModel
    {
        public string Email { get; set; }
        public string Etunimi { get; set; } // Tässä voit lisätä käyttäjän muita profiilitietoja
        public string Sukunimi { get; set; }

        // Salasanan vaihto
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}