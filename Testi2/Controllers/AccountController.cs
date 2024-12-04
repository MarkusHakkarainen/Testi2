using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using Testi2.Models;
using System.Security.Cryptography;
using System.Text;


namespace Testi2.Controllers
{
    public class AccountController : Controller
    {
        private readonly OmiajuttujaEntities db = new OmiajuttujaEntities();

        
        public ActionResult Login()
        {
            return View();
        }

        // Kirjautuminen POST-pyyntö
        [HttpPost]
        public ActionResult Login(string email)
        {
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Tätä sähköpostiosoitetta ei löydy.");
                return View();
            }

            // Jos käyttäjä on ensimmäistä kertaa kirjautumassa
            if (user.IsFirstLogin == true)
            {
                // Luo satunnainen salasana
                string temporaryPassword = GenerateRandomPassword();

                // Luo suola ja hashataan salasana
                string salt = GenerateSalt();
                string hashedPassword = HashPassword(temporaryPassword, salt);

                // Päivitä käyttäjän salasana tietokantaan
                user.PasswordHash = hashedPassword;
                user.Salt = salt;
                user.IsFirstLogin = false; // Merkitään, että ei ole enää ensimmäistä kertaa kirjautumassa
                db.SaveChanges();

                // Lähetetään sähköposti SQL Serverin kautta
                SendEmailFromSqlServer(user.Email, temporaryPassword);

                // Lähetetään viesti käyttäjälle
                ViewBag.Message = "Salasana on lähetetty sähköpostitse.";
                return RedirectToAction("ChangePassword");
            }

            // Jos käyttäjä on jo kirjautunut aiemmin
            return RedirectToAction("ChangePassword");
        }


        // Lähetä sähköposti SQL Serverin kautta
        private void SendEmailFromSqlServer(string email, string temporaryPassword)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));

            // Yhteysmerkkijono, joka viittaa tietokannan yhteyteen
            string connectionString = ConfigurationManager.ConnectionStrings["OmiajuttujaEntities"].ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("EXEC msdb.dbo.sp_send_dbmail @profile_name = 'Laukkarinvesiensuojeluyhdistys', @recipients = @recipients, @subject = 'Your Temporary Password', @body = @body, @body_format = 'HTML'", connection))
                {
                    command.Parameters.AddWithValue("@recipients", email); // Lähettäjän sähköpostiosoite
                    command.Parameters.AddWithValue("@body", $"Your temporary password is: <strong>{temporaryPassword}</strong>. Please log in and change it immediately."); // Sähköpostin sisältö
                    command.ExecuteNonQuery(); // Suoritetaan komento SQL Serverissä
                }
            }
        }


        // Luo satunnainen salasana
        private string GenerateRandomPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            Random random = new Random();
            return new string(Enumerable.Repeat(validChars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public ActionResult ChangePassword()
        {
            

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var email = Session["UserEmail"]?.ToString();
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("", "Et ole kirjautunut sisään.");
                return RedirectToAction("Login");
            }

            if (HashPassword(currentPassword, user.Salt) != user.PasswordHash)
            {
                ModelState.AddModelError("", "Nykyinen salasana on väärin.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Salasanat eivät täsmää.");
                return View();
            }

            // Päivitä salasana
            string salt = GenerateSalt();
            string hashedPassword = HashPassword(newPassword, salt);

            user.PasswordHash = hashedPassword;
            user.Salt = salt;
            user.IsFirstLogin = false; // Merkitään, että käyttäjä on vaihtanut salasanansa

            db.SaveChanges();

            // Tallenna onnistumisviesti (valinnainen)
            ViewBag.Message = "Salasana vaihdettu onnistuneesti!";

            // Ohjaa käyttäjä etusivulle
            return RedirectToAction("Index", "Home");
        }

        // Muokkaa profiilia ja muuta salasana (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Haetaan käyttäjä sessiosta
                var email = Session["UserEmail"]?.ToString();
                var user = db.Users.FirstOrDefault(u => u.Email == email);

                if (user != null)
                {
                    // Jos nykyinen salasana on annettu, tarkistetaan se
                    if (!string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        string hashedCurrentPassword = HashPassword(model.CurrentPassword, user.Salt);
                        if (hashedCurrentPassword != user.PasswordHash)
                        {
                            ModelState.AddModelError("", "Nykyinen salasana on väärin.");
                            return View(model);
                        }

                        // Tarkistetaan, että uusi salasana ja vahvistus ovat samat
                        if (model.NewPassword != model.ConfirmPassword)
                        {
                            ModelState.AddModelError("", "Uusi salasana ja sen vahvistus eivät täsmää.");
                            return View(model);
                        }

                        // Päivitetään salasana
                        string salt = GenerateSalt();
                        string hashedNewPassword = HashPassword(model.NewPassword, salt);

                        user.PasswordHash = hashedNewPassword;
                        user.Salt = salt;
                    }

                    //// Päivitetään profiilitiedot (esimerkiksi käyttäjän nimi)
                    //user.Name = model.Name; // Muokkaa tämä tarpeidesi mukaan, esimerkiksi lisäämällä muuta profiilitietoa

                    // Tallenna muutokset tietokantaan
                    db.SaveChanges();

                    ViewBag.Message = "Profiili ja salasana on päivitetty onnistuneesti!";
                    return RedirectToAction("EditProfile"); // Voit ohjata käyttäjän samaan näkymään tai etusivulle
                }

                ModelState.AddModelError("", "Käyttäjää ei löydy.");
            }

            return View(model);
        }

        // Luo suola
        private string GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] salt = new byte[16];
                rng.GetBytes(salt);
                return Convert.ToBase64String(salt);
            }
        }

        // Hashaa salasana
        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashedBytes = sha256.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
